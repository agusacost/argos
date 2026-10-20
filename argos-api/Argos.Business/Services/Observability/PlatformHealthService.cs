using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Observability;

/// <summary>
/// Prometheus solo scrapea nginx-exporter/node-exporter/cadvisor: la salud de api/rag-api/qdrant se
/// resuelve con probes HTTP directos sobre la red docker; <c>up{job}</c> solo alimenta la sparkline.
/// </summary>
public sealed class PlatformHealthService(
    IHttpProbe probe,
    IPrometheusClient prometheus,
    IObservabilityConfig config,
    IClock clock,
    ILogger<PlatformHealthService> log) : IPlatformHealthService
{
    private const int SparklineStepSeconds = 120;
    private static readonly TimeSpan SparklineWindow = TimeSpan.FromMinutes(24);

    private IEnumerable<HealthTarget> HealthTargets =>
    [
        new("Nginx Proxy", "nginx", "nginx-exporter", config.NginxStatusUrl),
        new("API .NET", "api", null, config.DotnetApiUrl),
        new("RAG API", "rag-api", null, config.RagSelfUrl),
        new("Qdrant", "qdrant", null, config.QdrantReadyUrl),
    ];

    private IEnumerable<IntegrationTarget> IntegrationTargets =>
    [
        new("prometheus", "Prometheus", $"{config.PrometheusUrl}/-/ready"),
        new("loki", "Loki", $"{config.LokiUrl}/ready"),
        new("alertmanager", "Alertmanager", $"{config.AlertmanagerUrl}/-/ready"),
        new("n8n", "n8n", $"{config.N8nUrl}/healthz"),
    ];

    public async Task<IReadOnlyList<ServiceHealth>> GetServicesHealthAsync()
    {
        var services = new List<ServiceHealth>();
        foreach (var target in HealthTargets)
            services.Add(new ServiceHealth(target.Name, target.Component, await StatusOfAsync(target.Url),
                await UpSparklineAsync(target.PrometheusJob)));
        return services;
    }

    public async Task<IReadOnlyList<IntegrationStatus>> GetIntegrationsStatusAsync()
    {
        var statuses = new List<IntegrationStatus>();
        foreach (var target in IntegrationTargets)
        {
            var result = await probe.ProbeAsync(target.Url);
            statuses.Add(new IntegrationStatus(
                target.Key,
                target.Name,
                result.StatusCode is < 300 ? "up" : "down",
                result.StatusCode,
                (long)Math.Round(result.Elapsed.TotalMilliseconds, MidpointRounding.ToEven),
                result.Detail));
        }
        return statuses;
    }

    private async Task<string> StatusOfAsync(string url)
    {
        var result = await probe.ProbeAsync(url);
        if (!result.Responded)
            log.LogWarning("[observability] probe {Url} error: {Error}", url, result.Detail);
        return result.StatusCode is < 500 ? "healthy" : "degraded";
    }

    private async Task<IReadOnlyList<double>> UpSparklineAsync(string? job)
    {
        if (job is null) return [];
        try
        {
            var now = clock.UtcNow;
            return await prometheus.QueryRangeAsync($"up{{job=\"{job}\"}}", now - SparklineWindow, now, SparklineStepSeconds);
        }
        catch (Exception)
        {
            return [];
        }
    }
}
