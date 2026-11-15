using Argos.Entities;

namespace Argos.Business.Services.Rag;

/// <summary>
/// El nombre igual es una señal que SUMA, no un filtro que excluye:
/// <list type="number">
/// <item>Conocida por similitud: algún candidato con conocimiento supera el umbral (criterio histórico; puede ser de
/// otro tipo, p. ej. HostCriticalCPU contra el runbook de HostHighCPU, 0,789).</item>
/// <item>Si no, conocida por nombre: un candidato del MISMO <c>tipo_alerta</c>, con conocimiento y con solución
/// (documento o pasos), cuya similitud alcanza el piso (p. ej. NginxSuspiciousIPActivity, 0,607: su texto cambia con
/// la IP y el valor medido).</item>
/// <item>Si no, nueva: un documento de OTRA alerta parecido pero bajo el umbral no la hace conocida (NginxDown contra
/// NginxHighResponseTime, 0,669).</item>
/// </list>
/// </summary>
public sealed class KnowledgeClassifier : IKnowledgeClassifier
{
    public KnowledgeClassification Classify(IReadOnlyList<ScoredIncident> candidates, string? alertname, double threshold,
        double nameMatchMinScore)
    {
        // Entre los que califican, primero los que tienen cómo resolverse (los copia la rama 'conocida').
        var bySimilarity = candidates
            .Where(c => c.Score > threshold && c.Incident.CountsAsKnowledge)
            .OrderByDescending(c => c.Incident.HasSolution)
            .ThenByDescending(c => c.Score)
            .FirstOrDefault();
        if (bySimilarity is not null)
            return new KnowledgeClassification(true, KnowledgeMatchReasons.Similarity, bySimilarity);

        var name = alertname?.Trim();
        if (string.IsNullOrEmpty(name)) return KnowledgeClassification.New;

        var byName = candidates
            .Where(c => string.Equals(c.Incident.TipoAlerta, name, StringComparison.Ordinal) &&
                        c.Incident.CountsAsKnowledge && c.Incident.HasSolution &&
                        c.Score >= nameMatchMinScore)
            .OrderByDescending(c => c.Score)
            .FirstOrDefault();
        return byName is null
            ? KnowledgeClassification.New
            : new KnowledgeClassification(true, KnowledgeMatchReasons.AlertName, byName);
    }
}
