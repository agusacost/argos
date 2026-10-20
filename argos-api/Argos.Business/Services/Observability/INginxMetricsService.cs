using Argos.Entities;

namespace Argos.Business.Services.Observability;

public interface INginxMetricsService
{
    /// <summary>Golden signals de Nginx en el rango (15m, 1h, 3h o 6h; otro valor = 1h).</summary>
    Task<NginxMetrics> GetAsync(string range);
}
