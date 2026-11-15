using Argos.Entities;

namespace Argos.Business.Services.Rag;

/// <summary>Regla pura conocida/nueva de una alerta a partir de los candidatos del RAG.</summary>
public interface IKnowledgeClassifier
{
    /// <param name="candidates">Candidatos con su similitud (top-k y, si los hay, los del mismo tipo).</param>
    /// <param name="alertname">Nombre de la alerta; null o vacío = solo similitud (comportamiento anterior).</param>
    /// <param name="threshold">Umbral de similitud vigente.</param>
    /// <param name="nameMatchMinScore">Piso de similitud para la coincidencia por nombre.</param>
    KnowledgeClassification Classify(IReadOnlyList<ScoredIncident> candidates, string? alertname, double threshold,
        double nameMatchMinScore);
}
