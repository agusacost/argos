using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Argos.Common.Serialization;
using Argos.Entities;

namespace Argos.Repository.Qdrant;

/// <summary>
/// Mismo formato JSON que la API (snake_case, nulls explícitos) sin la propiedad <c>id</c> de las
/// entidades: el id va en el punto de Qdrant, no en el payload.
/// </summary>
public sealed class PayloadSerializer : IPayloadSerializer
{
    private readonly JsonSerializerOptions _options;

    public PayloadSerializer()
    {
        _options = JsonDefaults.Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _options.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { RemoveEntityId } };
    }

    public JsonObject Serialize<T>(T value) =>
        JsonSerializer.SerializeToNode(value, _options)?.AsObject()
        ?? throw new InvalidOperationException($"No se pudo serializar {typeof(T).Name}");

    public T Deserialize<T>(JsonObject payload) =>
        payload.Deserialize<T>(_options) ?? throw new InvalidOperationException($"Payload vacío para {typeof(T).Name}");

    private static void RemoveEntityId(JsonTypeInfo typeInfo)
    {
        if (!typeof(IEntity).IsAssignableFrom(typeInfo.Type)) return;
        var id = typeInfo.Properties.FirstOrDefault(p => p.Name == "id");
        if (id is not null) typeInfo.Properties.Remove(id);
    }
}
