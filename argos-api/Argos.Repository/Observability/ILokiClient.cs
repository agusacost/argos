namespace Argos.Repository.Observability;

/// <summary>Consultas a Loki. Lanzan excepción si Loki no responde.</summary>
public interface ILokiClient
{
    /// <summary>Líneas de log del selector en la ventana, en el orden en que las devuelve Loki (backward).</summary>
    Task<IReadOnlyList<string>> QueryLinesAsync(string selector, DateTimeOffset start, DateTimeOffset end, int limit);

    /// <summary>Query de métrica LogQL (primera serie) → {timestamp en segundos: valor}.</summary>
    Task<IReadOnlyDictionary<long, double>> QueryMatrixAsync(string query, DateTimeOffset start, DateTimeOffset end, int stepSeconds);
}
