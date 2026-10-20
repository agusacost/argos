using Argos.Common.Configuration;
using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Embeddings;
using Argos.Repository.Qdrant;
using Microsoft.Extensions.Logging;

namespace Argos.Repository.Repositories;

/// <summary>
/// Colección vectorial de incidentes. El vector es el embedding del resumen y el id es
/// <c>uuid5(NAMESPACE_DNS, "tipo::resumen")</c>: re-guardar el mismo incidente no lo duplica.
/// </summary>
public sealed class IncidentRepository(
    IQdrantClient client,
    IPayloadSerializer serializer,
    ITextEmbedder embedder,
    IIdGenerator ids,
    IQdrantConfig config,
    ILogger<IncidentRepository> log)
    : QdrantRepository<Incident>(client, serializer), IIncidentRepository
{
    protected override string CollectionName => config.CollectionName;
    protected override int VectorSize => config.VectorSize;

    /// <summary><c>Distance[config.DISTANCE.upper()]</c> → "Cosine" / "Dot" / "Euclid".</summary>
    protected override string Distance
    {
        get
        {
            var d = config.Distance;
            return d.Length == 0 ? "Cosine" : char.ToUpperInvariant(d[0]) + d[1..].ToLowerInvariant();
        }
    }

    protected override string AssignId(Incident entity) => ids.FromName(entity.NaturalKey);

    protected override IReadOnlyList<float> VectorOf(Incident entity) => embedder.Embed(entity.EmbeddingText);

    protected override void OnCollectionCreated() =>
        log.LogInformation("Colección '{Col}' creada (size={Size}, distance={Distance}).", CollectionName, VectorSize, Distance);

    public async Task<IReadOnlyList<ScoredIncident>> SearchSimilarAsync(string text, int limit)
    {
        var hits = await Client.SearchAsync(CollectionName, embedder.Embed(text), limit);
        return hits.Select(h => new ScoredIncident(ToEntity(h), h.Score)).ToList();
    }

    public Task<IReadOnlyList<Incident>> FindAsync(IncidentFilter filter) =>
        FindAsync(new QdrantFilter()
            .MatchValue("estado", filter.Estado)
            .MatchAny("estado", filter.Estados)
            .MatchValue("tipo_alerta", filter.Tipo)
            .MatchValue("fuente", filter.Fuente));
}
