using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Argos.Api.Binding;

/// <summary>Entero tolerante: número sin decimales (3 o 3.0) o string numérico ("3").</summary>
public sealed class LenientInt32Converter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        double value = reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
            _ => throw new JsonException("Se esperaba un entero"),
        };
        if (value != Math.Floor(value) || value is > int.MaxValue or < int.MinValue)
            throw new JsonException("Se esperaba un entero");
        return (int)value;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
