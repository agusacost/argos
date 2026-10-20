using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

public sealed class LoadTestRunRepository(IQdrantClient client, IPayloadSerializer serializer, IIdGenerator ids)
    : MetadataRepository<LoadTestRun>(client, serializer, ids), ILoadTestRunRepository
{
    protected override string CollectionName => CollectionNames.LoadTests;

    public async Task<LoadTestRun?> FindRunningAsync() =>
        (await FindAsync(new QdrantFilter().MatchValue("status", LoadTestStatuses.Running))).FirstOrDefault();
}
