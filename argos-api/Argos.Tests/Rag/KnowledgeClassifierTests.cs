using Argos.Business.Services.Rag;
using Argos.Entities;

namespace Argos.Tests.Rag;

/// <summary>Casos medidos en la evaluación del OE3 (docs/EVALUACION_OE3.md §3), umbral 0.68 y piso 0.50.</summary>
public sealed class KnowledgeClassifierTests
{
    private const double Threshold = 0.68;
    private const double Floor = 0.50;
    private readonly KnowledgeClassifier _classifier = new();

    private static ScoredIncident Runbook(string tipo, double score, bool withSolution = true) =>
        new(new Incident
        {
            Id = Guid.NewGuid().ToString(),
            TipoAlerta = tipo,
            Resumen = $"runbook {tipo}",
            Pasos = withSolution ? ["paso 1"] : [],
            Estado = IncidentStates.DocumentadoPreviamente,
        }, score);

    [Fact]
    public void F6_SameAlertBelowThreshold_IsKnownByName()
    {
        var own = Runbook("NginxSuspiciousIPActivity", 0.607);
        var result = _classifier.Classify([Runbook("NginxHighErrorRate", 0.55), own], "NginxSuspiciousIPActivity",
            Threshold, Floor);

        Assert.True(result.IsKnown);
        Assert.Equal(KnowledgeMatchReasons.AlertName, result.Reason);
        Assert.Same(own, result.Best);
    }

    [Fact]
    public void F6_WithoutAlertname_KeepsThePreviousBehaviour()
    {
        var result = _classifier.Classify([Runbook("NginxSuspiciousIPActivity", 0.607)], null, Threshold, Floor);

        Assert.False(result.IsKnown);
        Assert.Equal(KnowledgeMatchReasons.None, result.Reason);
    }

    [Fact]
    public void F5_OtherAlertAboveThreshold_IsStillKnownBySimilarity()
    {
        var result = _classifier.Classify([Runbook("HostHighCPU", 0.789)], "HostCriticalCPU", Threshold, Floor);

        Assert.True(result.IsKnown);
        Assert.Equal(KnowledgeMatchReasons.Similarity, result.Reason);
    }

    [Fact]
    public void F7_SimilarOtherAlertBelowThreshold_IsNew()
    {
        var result = _classifier.Classify([Runbook("NginxHighResponseTime", 0.669), Runbook("NginxHighErrorRate", 0.6)],
            "NginxDown", Threshold, Floor);

        Assert.False(result.IsKnown);
        Assert.Null(result.Best);
    }

    [Fact]
    public void SameAlertBelowFloor_IsNew()
    {
        var result = _classifier.Classify([Runbook("NginxSuspiciousIPActivity", 0.42)], "NginxSuspiciousIPActivity",
            Threshold, Floor);

        Assert.False(result.IsKnown);
    }

    [Fact]
    public void SameAlertWithoutKnowledge_IsNew()
    {
        var noSolution = Runbook("CAdvisorDown", 0.6, withSolution: false);
        Assert.False(_classifier.Classify([noSolution], "CAdvisorDown", Threshold, Floor).IsKnown);

        // Un 'nuevo' sin documento no es conocimiento ni por similitud ni por nombre.
        var pending = new ScoredIncident(new Incident
        {
            TipoAlerta = "CAdvisorDown", Resumen = "x", Estado = IncidentStates.Nuevo, Pasos = ["algo"],
        }, 0.9);
        Assert.False(_classifier.Classify([pending], "CAdvisorDown", Threshold, Floor).IsKnown);
    }

    [Fact]
    public void NameComparisonIsExact()
    {
        var result = _classifier.Classify([Runbook("NginxSuspiciousIPActivity", 0.6)], "nginxsuspiciousipactivity",
            Threshold, Floor);

        Assert.False(result.IsKnown);
    }

    [Fact]
    public void FloorAboveOne_DisablesTheNameSignal()
    {
        var result = _classifier.Classify([Runbook("NginxSuspiciousIPActivity", 0.6)], "NginxSuspiciousIPActivity",
            Threshold, 1.01);

        Assert.False(result.IsKnown);
    }

    [Fact]
    public void Similarity_PrefersCandidatesWithSolution()
    {
        var withoutSolution = new ScoredIncident(new Incident
        {
            TipoAlerta = "A", Resumen = "a", Estado = IncidentStates.Resuelto,
        }, 0.95);
        var withSolution = Runbook("A", 0.80);

        var result = _classifier.Classify([withoutSolution, withSolution], "A", Threshold, Floor);

        Assert.Equal(KnowledgeMatchReasons.Similarity, result.Reason);
        Assert.Same(withSolution, result.Best);
    }
}
