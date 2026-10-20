namespace Argos.Repository.Observability;

/// <summary>Consultas a Prometheus. Lanzan excepción si Prometheus no responde o rechaza la query.</summary>
public interface IPrometheusClient
{
    /// <summary>Valores de la primera serie de un <c>query_range</c>.</summary>
    Task<IReadOnlyList<double>> QueryRangeAsync(string expr, DateTimeOffset start, DateTimeOffset end, int stepSeconds);
}
