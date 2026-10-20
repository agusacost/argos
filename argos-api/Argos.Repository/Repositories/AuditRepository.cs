using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

public sealed class AuditRepository(IQdrantClient client, IPayloadSerializer serializer, IIdGenerator ids)
    : MetadataRepository<AuditEntry>(client, serializer, ids), IAuditRepository
{
    protected override string CollectionName => CollectionNames.Audit;

    public Task<IReadOnlyList<AuditEntry>> ListByIncidentAsync(string incidentId) =>
        FindAsync(new QdrantFilter().MatchValue("incident_id", incidentId));
}
