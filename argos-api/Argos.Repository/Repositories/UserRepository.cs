using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

public sealed class UserRepository(IQdrantClient client, IPayloadSerializer serializer, IIdGenerator ids)
    : MetadataRepository<User>(client, serializer, ids), IUserRepository
{
    protected override string CollectionName => CollectionNames.Users;

    public async Task<User?> GetByEmailAsync(string email) =>
        (await FindAsync(new QdrantFilter().MatchValue("email", email))).FirstOrDefault();
}
