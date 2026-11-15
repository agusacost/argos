using Argos.Business.Services.Settings;
using Argos.Common.Configuration;
using Argos.Entities;
using Argos.Repository.Repositories;

namespace Argos.Business.Services.Rag;

public sealed class KnowledgeMatcher(
    IIncidentRepository incidents,
    ISettingsService settings,
    IKnowledgeClassifier classifier,
    IQdrantConfig config) : IKnowledgeMatcher
{
    // Ocurrencias del mismo tipo que se miran además del top-k (las más parecidas primero).
    private const int SameAlertCandidates = 3;

    public async Task<KnowledgeLookup> LookupAsync(string text, string? alertname, int topK)
    {
        var threshold = await settings.GetSimilarityThresholdAsync();
        var floor = config.NameMatchMinScore;
        var candidates = (await incidents.SearchSimilarAsync(text, topK)).ToList();

        // El documento del mismo tipo puede no estar en el top-k (otro tipo más parecido): se busca aparte.
        var name = alertname?.Trim();
        if (!string.IsNullOrEmpty(name) && floor <= 1)
        {
            foreach (var hit in await incidents.SearchSimilarAsync(text, SameAlertCandidates, name))
                if (!candidates.Any(c => SamePoint(c, hit))) candidates.Add(hit);
        }

        var classification = classifier.Classify(candidates, name, threshold, floor);
        return new KnowledgeLookup(candidates, threshold, floor, classification);
    }

    private static bool SamePoint(ScoredIncident a, ScoredIncident b) =>
        a.Incident.Id is { } id ? id == b.Incident.Id : ReferenceEquals(a.Incident, b.Incident);
}
