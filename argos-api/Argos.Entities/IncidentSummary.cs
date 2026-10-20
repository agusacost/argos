using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>
/// Incidente sin el documento markdown: es lo que devuelven los listados paginados.
/// </summary>
public class IncidentSummary : Entity
{
    public string TipoAlerta { get; set; } = "";
    public string Resumen { get; set; } = "";
    public List<string> Pasos { get; set; } = [];
    public string? Fecha { get; set; }
    public string Estado { get; set; } = IncidentStates.DocumentadoPreviamente;
    public string Fuente { get; set; } = "manual";
    public string? Severity { get; set; }
    public string? DetectedAt { get; set; }
    public string? ResolvedAt { get; set; }
    public double? MttrMinutes { get; set; }

    public IncidentSummary()
    {
    }

    public IncidentSummary(IncidentSummary source) : base(source)
    {
        TipoAlerta = source.TipoAlerta;
        Resumen = source.Resumen;
        Pasos = [.. source.Pasos];
        Fecha = source.Fecha;
        Estado = source.Estado;
        Fuente = source.Fuente;
        Severity = source.Severity;
        DetectedAt = source.DetectedAt;
        ResolvedAt = source.ResolvedAt;
        MttrMinutes = source.MttrMinutes;
    }

    /// <summary>Fecha de referencia del incidente: <c>detected_at</c> o, si falta, <c>fecha</c>.</summary>
    [JsonIgnore]
    public string? ReferenceDate =>
        !string.IsNullOrEmpty(DetectedAt) ? DetectedAt : !string.IsNullOrEmpty(Fecha) ? Fecha : null;

    /// <summary>Genera notificación: incidentes nuevos o de severidad crítica.</summary>
    [JsonIgnore]
    public bool RequiresNotification => Estado == IncidentStates.Nuevo || Severity == "critical";
}
