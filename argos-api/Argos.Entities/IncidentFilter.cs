namespace Argos.Entities;

/// <summary>Criterios del listado de incidentes (filtros de Qdrant + búsqueda de texto + paginado).</summary>
public sealed class IncidentFilter
{
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
    public string? Estado { get; init; }
    public IReadOnlyList<string>? Estados { get; init; }

    /// <summary>Tipo de alerta EXACTO (filtro de Qdrant; lo usa el ciclo de vida de alertas).</summary>
    public string? Tipo { get; init; }

    /// <summary>Parte del tipo de alerta, sin distinguir mayúsculas (<c>GET /incidents?tipo=</c> del panel).</summary>
    public string? TipoContains { get; init; }

    public string? Fuente { get; init; }
    public string? Search { get; init; }

    /// <summary>
    /// Filtros de texto que se aplican en memoria: <see cref="TipoContains"/> sobre el tipo y <see cref="Search"/>
    /// sobre el resumen o el tipo, ambos por subcadena y sin distinguir mayúsculas.
    /// </summary>
    public bool MatchesText(IncidentSummary incident) =>
        Contains(incident.TipoAlerta, TipoContains) &&
        (string.IsNullOrEmpty(Search) || Contains(incident.Resumen, Search) || Contains(incident.TipoAlerta, Search));

    private static bool Contains(string value, string? needle) =>
        string.IsNullOrEmpty(needle) || value.Contains(needle.Trim(), StringComparison.OrdinalIgnoreCase);
}
