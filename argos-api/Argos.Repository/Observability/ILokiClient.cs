namespace Argos.Repository.Observability;

/// <summary>Consultas a Loki. Lanzan excepción si Loki no responde.</summary>
public interface ILokiClient
{
    /// <summary>Líneas de log del selector en la ventana, en el orden en que las devuelve Loki (backward).</summary>
    Task<IReadOnlyList<string>> QueryLinesAsync(string selector, DateTimeOffset start, DateTimeOffset end, int limit);

    /// <summary>Query de métrica LogQL (primera serie) → {timestamp en segundos: valor}.</summary>
    /// <summary>Query de métrica LogQL instantánea (<c>/query</c>) → todas las series con sus labels.</summary>
    Task<IReadOnlyList<LokiSample>> QueryVectorAsync(string query, DateTimeOffset time);

    Task<IReadOnlyDictionary<long, double>> QueryMatrixAsync(string query, DateTimeOffset start, DateTimeOffset end, int stepSeconds);
}
