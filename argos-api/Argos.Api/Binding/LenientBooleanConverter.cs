using System.Text.Json;
using System.Text.Json.Serialization;

namespace Argos.Api.Binding;

/// <summary>Booleano tolerante: true/false o los números 0 y 1.</summary>
public sealed class LenientBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.GetDouble() is 0 or 1 => reader.GetDouble() == 1,
            _ => throw new JsonException("Se esperaba un booleano"),
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}
