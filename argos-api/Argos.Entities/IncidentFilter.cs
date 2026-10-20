namespace Argos.Entities;

/// <summary>Criterios del listado de incidentes (filtros de Qdrant + búsqueda de texto + paginado).</summary>
public sealed class IncidentFilter
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Estado { get; init; }
    public IReadOnlyList<string>? Estados { get; init; }
    public string? Tipo { get; init; }
    public string? Fuente { get; init; }
    public string? Search { get; init; }
}
