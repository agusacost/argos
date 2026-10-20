using System.Text.Json.Nodes;

namespace Argos.Repository.Qdrant;

/// <summary>
/// Filtro <c>must</c> de Qdrant (FieldCondition con MatchValue / MatchAny). Las condiciones con valor
/// vacío se omiten.
/// </summary>
public sealed class QdrantFilter
{
    private readonly List<JsonObject> _conditions = [];

    public bool IsEmpty => _conditions.Count == 0;

    public QdrantFilter MatchValue(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _conditions.Add(new JsonObject { ["key"] = key, ["match"] = new JsonObject { ["value"] = value } });
        return this;
    }

    public QdrantFilter MatchAny(string key, IReadOnlyList<string>? values)
    {
        if (values is { Count: > 0 })
        {
            var any = new JsonArray();
            foreach (var v in values) any.Add(v);
            _conditions.Add(new JsonObject { ["key"] = key, ["match"] = new JsonObject { ["any"] = any } });
        }
        return this;
    }

    public JsonObject ToJson()
    {
        var must = new JsonArray();
        foreach (var c in _conditions) must.Add(c.DeepClone());
        return new JsonObject { ["must"] = must };
    }
}
