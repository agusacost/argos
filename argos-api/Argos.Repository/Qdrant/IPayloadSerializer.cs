using System.Text.Json.Nodes;

namespace Argos.Repository.Qdrant;

/// <summary>Conversión entre entidades y payloads de Qdrant (el id va en el punto, no en el payload).</summary>
public interface IPayloadSerializer
{
    JsonObject Serialize<T>(T value);

    T Deserialize<T>(JsonObject payload);
}
