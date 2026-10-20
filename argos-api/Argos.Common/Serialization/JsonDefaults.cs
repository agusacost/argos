using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Argos.Common.Serialization;

/// <summary>
/// Formato JSON del contrato (respuestas de la API y payloads de Qdrant): snake_case, nulls explícitos
/// y sin escapar acentos.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions o)
    {
        o.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.DictionaryKeyPolicy = null;
        o.PropertyNameCaseInsensitive = false;
        o.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        o.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        o.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        return o;
    }
}
