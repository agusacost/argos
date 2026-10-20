using Argos.Business.Services.Observability;
using Argos.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

[Route("observability")]
public sealed class ObservabilityController(IPlatformHealthService health, INginxMetricsService nginx) : ArgosControllerBase
{
    private const string DefaultRange = "1h";

    /// <summary>"Salud de Servicios" de la vista Métricas.</summary>
    [HttpGet("services")]
    public Task<IReadOnlyList<ServiceHealth>> Services() => health.GetServicesHealthAsync();

    /// <summary>Golden signals de Nginx (status codes, latencia P50/P95/P99, error rate).</summary>
    [HttpGet("nginx-metrics")]
    public Task<NginxMetrics> NginxMetrics([FromQuery] string? range = DefaultRange) => nginx.GetAsync(range ?? DefaultRange);
}
