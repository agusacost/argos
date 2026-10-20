using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

public sealed class NotificationRepository(IQdrantClient client, IPayloadSerializer serializer, IIdGenerator ids)
    : MetadataRepository<Notification>(client, serializer, ids), INotificationRepository
{
    protected override string CollectionName => CollectionNames.Notifications;
}
