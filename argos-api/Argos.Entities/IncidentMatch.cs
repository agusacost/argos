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

    /// <param name="incident">Incidente devuelto por Qdrant.</param>
    /// <param name="score">Similitud con la consulta.</param>
    /// <param name="threshold">Umbral vigente.</param>
    /// <param name="knownByAlertName">Es el documento que hizo conocida la alerta por coincidencia de nombre.</param>
    public IncidentMatch(Incident incident, double score, double threshold, bool knownByAlertName = false) : base(incident)
    {
        Id = null;
        Score = score;
        Clasificacion = score > threshold || knownByAlertName ? Documentado : Nuevo;
    }
}
