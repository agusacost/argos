using System.Text.Json.Serialization;
using Argos.Common.Time;

namespace Argos.Entities;

/// <summary>Incidente completo, tal como se guarda en la colección vectorial de Qdrant.</summary>
public class Incident : IncidentSummary
{
    public string? Documento { get; set; }

    public Incident()
    {
    }

    protected Incident(Incident source) : base(source)
    {
        Documento = source.Documento;
    }

    /// <summary>Texto que se vectoriza para la búsqueda semántica.</summary>
    [JsonIgnore]
    public string EmbeddingText => Resumen;

    /// <summary>Clave del id determinístico: re-guardar el mismo incidente sobrescribe el mismo punto.</summary>
    [JsonIgnore]
    public string NaturalKey => $"{TipoAlerta}::{Resumen}";

    /// <summary>Un incidente 'nuevo' sin documento (auto-doc desactivada) no cuenta como conocimiento.</summary>
    [JsonIgnore]
    public bool CountsAsKnowledge => Estado != IncidentStates.Nuevo || !string.IsNullOrEmpty(Documento);

    public IncidentSummary ToSummary() => new(this);

    /// <summary>Aplica los campos informados (los null se ignoran) y recalcula el MTTR.</summary>
    public void ApplyChanges(string? estado = null, string? resolvedAt = null, string? documento = null,
        IReadOnlyList<string>? pasos = null)
    {
        if (estado is not null) Estado = estado;
        if (resolvedAt is not null) ResolvedAt = resolvedAt;
        if (documento is not null) Documento = documento;
        if (pasos is not null) Pasos = [.. pasos];
        RecalculateMttr();
    }

    /// <summary>MTTR en minutos (2 decimales) si hay <c>detected_at</c> y <c>resolved_at</c> válidos.</summary>
    public void RecalculateMttr()
    {
        var detected = IsoDateTime.Parse(DetectedAt);
        var resolved = IsoDateTime.Parse(ResolvedAt);
        if (detected is not null && resolved is not null)
            MttrMinutes = RoundingMath.Round((resolved.Value - detected.Value).TotalSeconds / 60.0, 2);
    }
}
