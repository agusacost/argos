using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

/// <summary>
/// Repositorio base sobre una colección de Qdrant. Las subclases definen la colección, cómo se
/// identifica cada entidad y qué vector se indexa (template method).
/// </summary>
public abstract class QdrantRepository<T>(IQdrantClient client, IPayloadSerializer serializer) : IRepository<T>
    where T : Entity
{
    protected IQdrantClient Client { get; } = client;
    protected IPayloadSerializer Serializer { get; } = serializer;

    protected abstract string CollectionName { get; }
    protected abstract int VectorSize { get; }
    protected abstract string Distance { get; }

    /// <summary>Id del punto donde se guarda la entidad.</summary>
    protected abstract string AssignId(T entity);

    /// <summary>Vector que se indexa junto con la entidad.</summary>
    protected abstract IReadOnlyList<float> VectorOf(T entity);

    /// <summary>Se llama después de crear la colección.</summary>
    protected virtual void OnCollectionCreated()
    {
    }

    public async Task EnsureCollectionAsync()
    {
        var existing = await Client.GetCollectionNamesAsync();
        if (existing.Contains(CollectionName)) return;
        await Client.CreateCollectionAsync(CollectionName, VectorSize, Distance);
        OnCollectionCreated();
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        var point = await Client.RetrieveAsync(CollectionName, id);
        return point is null ? null : ToEntity(point);
    }

    public Task<IReadOnlyList<T>> ListAsync() => FindAsync(null);

    public async Task<T> SaveAsync(T entity)
    {
        entity.Id = AssignId(entity);
        await Client.UpsertAsync(CollectionName, entity.Id, VectorOf(entity), Serializer.Serialize(entity));
        return entity;
    }

    public Task UpdateAsync(T entity)
    {
        if (string.IsNullOrEmpty(entity.Id))
            throw new InvalidOperationException($"{typeof(T).Name} sin id: no se puede actualizar");
        return Client.SetPayloadAsync(CollectionName, entity.Id, Serializer.Serialize(entity));
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (await Client.RetrieveAsync(CollectionName, id) is null) return false;
        await Client.DeleteAsync(CollectionName, id);
        return true;
    }

    protected async Task<IReadOnlyList<T>> FindAsync(QdrantFilter? filter)
    {
        var points = await Client.ScrollAsync(CollectionName, filter);
        return points.Select(ToEntity).ToList();
    }

    protected T ToEntity(QdrantPoint point)
    {
        var entity = Serializer.Deserialize<T>(point.Payload);
        entity.Id = point.Id;
        return entity;
    }
}
