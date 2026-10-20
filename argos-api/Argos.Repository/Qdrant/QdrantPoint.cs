using System.Text.Json.Nodes;

namespace Argos.Repository.Qdrant;

/// <summary>Punto de Qdrant: id, payload crudo y score (solo en búsquedas).</summary>
public sealed record QdrantPoint(string Id, JsonObject Payload, double Score = 0);
