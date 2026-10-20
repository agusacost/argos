using System.Text.Json.Nodes;

namespace Argos.Repository.Qdrant;

/// <summary>Operaciones de Qdrant que usa Argos (REST, puerto 6333).</summary>
public interface IQdrantClient
{
    Task<IReadOnlySet<string>> GetCollectionNamesAsync(CancellationToken ct = default);

    Task CreateCollectionAsync(string name, int vectorSize, string distance, CancellationToken ct = default);

    Task UpsertAsync(string collection, string id, IReadOnlyList<float> vector, JsonObject payload, CancellationToken ct = default);

    Task<IReadOnlyList<QdrantPoint>> SearchAsync(string collection, IReadOnlyList<float> vector, int limit, CancellationToken ct = default);

    /// <summary>Recorre la colección completa (paginando) con el filtro opcional.</summary>
    Task<IReadOnlyList<QdrantPoint>> ScrollAsync(string collection, QdrantFilter? filter = null, CancellationToken ct = default);

    /// <summary>null si no existe. Un id que no es UUID se trata como inexistente.</summary>
    Task<QdrantPoint?> RetrieveAsync(string collection, string id, CancellationToken ct = default);

    /// <summary>Mergea las claves dadas sobre el payload existente.</summary>
    Task SetPayloadAsync(string collection, string id, JsonObject payload, CancellationToken ct = default);

    Task DeleteAsync(string collection, string id, CancellationToken ct = default);
}
