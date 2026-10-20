namespace Argos.Entities;

/// <summary>Incidente devuelto por Qdrant junto con su similitud con la consulta.</summary>
public sealed record ScoredIncident(Incident Incident, double Score);
