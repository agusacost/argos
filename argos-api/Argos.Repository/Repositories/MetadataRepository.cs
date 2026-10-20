using Argos.Common.Identity;
using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

/// <summary>
/// Colecciones de datos no vectoriales (usuarios, auditoría, notificaciones, corridas de k6): vector
/// dummy <c>[1.0]</c> (size 1, Dot) y los datos en el payload. Id uuid4 si la entidad no tiene uno.
/// </summary>
public abstract class MetadataRepository<T>(IQdrantClient client, IPayloadSerializer serializer, IIdGenerator ids)
    : QdrantRepository<T>(client, serializer)
    where T : Entity
{
    private static readonly float[] DummyVector = [1.0f];

    protected override int VectorSize => 1;
    protected override string Distance => "Dot";

    protected override string AssignId(T entity) => string.IsNullOrEmpty(entity.Id) ? ids.NewId() : entity.Id;

    protected override IReadOnlyList<float> VectorOf(T entity) => DummyVector;
}
