using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Observability;

public sealed class IncidentObservabilityService(
    ILokiClient loki,
    IPrometheusClient prometheus,
    IAlertComponentResolver components,
    IClock clock,
    ILogger<IncidentObservabilityService> log) : IIncidentObservabilityService
{
    private const int MaxLogLines = 25;
    private const int SparklineStepSeconds = 120;
    private static readonly TimeSpan HalfWindow = TimeSpan.FromMinutes(30);

    public async Task<IncidentObservability> GetAsync(Incident incident)
    {
        var component = components.ComponentOf(incident.TipoAlerta);
        var logs = LogsAsync(component, incident.ReferenceDate);
        var sparkline = SparklineAsync(component, incident.ReferenceDate);
        return new IncidentObservability(await logs, await sparkline);
    }

    private async Task<IReadOnlyList<string>> LogsAsync(string component, string? referenceDate)
    {
        var (start, end) = Window(referenceDate, TimeSpan.FromHours(1));
        try
        {
            var lines = await loki.QueryLinesAsync($"{{service=\"{components.LokiServiceOf(component)}\"}}", start, end, MaxLogLines);
            return lines.Reverse().Take(MaxLogLines).ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning("[observability] Loki error: {Error}", ex.Message);
            return [];
        }
    }

    private async Task<IReadOnlyList<double>> SparklineAsync(string component, string? referenceDate)
    {
        if (components.PrometheusExpressionOf(component) is not { } expr) return [];
        var (start, end) = Window(referenceDate, HalfWindow);
        try
        {
            return await prometheus.QueryRangeAsync(expr, start, end, SparklineStepSeconds);
        }
        catch (Exception ex)
        {
            log.LogWarning("[observability] Prometheus sparkline error: {Error}", ex.Message);
            return [];
        }
    }

    /// <summary>±30 min alrededor del incidente; sin fecha válida, la última <paramref name="fallback"/>.</summary>
    private (DateTimeOffset Start, DateTimeOffset End) Window(string? referenceDate, TimeSpan fallback)
    {
        if (IsoDateTime.Parse(referenceDate) is { } ts) return (ts - HalfWindow, ts + HalfWindow);
        var now = clock.UtcNow;
        return (now - fallback, now);
    }
}
