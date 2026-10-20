using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>Resultado de la búsqueda semántica: <c>{"score", "clasificacion", **payload}</c> (sin id).</summary>
public sealed class IncidentMatch : Incident
{
    public const string Documentado = "documentado";
    public const string Nuevo = "nuevo";

    [JsonPropertyOrder(-9)]
    public double Score { get; set; }

    [JsonPropertyOrder(-8)]
    public string Clasificacion { get; set; } = Nuevo;

    public IncidentMatch()
    {
    }

    public IncidentMatch(Incident incident, double score, double threshold) : base(incident)
    {
        Id = null;
        Score = score;
        Clasificacion = score > threshold ? Documentado : Nuevo;
    }

    /// <summary>Supera el umbral y además es conocimiento útil.</summary>
    public bool IsDocumentedAbove(double threshold) => Score > threshold && CountsAsKnowledge;
}
