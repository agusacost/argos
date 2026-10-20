using System.Text.Json;
using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>
/// Base de las entidades persistidas. Los campos del payload que la entidad no declara se conservan en
/// <see cref="ExtraFields"/> para no perderlos al reescribir el documento.
/// </summary>
public abstract class Entity : IEntity
{
    [JsonPropertyOrder(-10)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }

    protected Entity()
    {
    }

    protected Entity(Entity source)
    {
        Id = source.Id;
        ExtraFields = source.ExtraFields is null ? null : new Dictionary<string, JsonElement>(source.ExtraFields);
    }
}
