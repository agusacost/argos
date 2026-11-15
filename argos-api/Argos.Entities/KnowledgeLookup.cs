namespace Argos.Entities;

/// <summary>
/// Búsqueda en la base de conocimiento para una alerta: candidatos (top-k por similitud y, si se informó el nombre,
/// el mejor del mismo tipo), parámetros usados y la clasificación.
/// </summary>
public sealed record KnowledgeLookup(
    IReadOnlyList<ScoredIncident> Candidates,
    double Threshold,
    double NameMatchMinScore,
    KnowledgeClassification Classification);
