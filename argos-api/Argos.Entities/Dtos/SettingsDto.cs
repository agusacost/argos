using System.Text.Json.Serialization;

namespace Argos.Entities.Dtos;

/// <summary>Configuración + qué API keys de LLM hay en el servidor (+ advertencia de la validación del modelo, si hubo).</summary>
public sealed class SettingsDto : AppSettings
{
    /// <summary>Proveedor → tiene API key cargada (solo lectura).</summary>
    public IReadOnlyDictionary<string, bool> LlmKeys { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Warning { get; }

    public SettingsDto(AppSettings settings, IReadOnlyDictionary<string, bool> llmKeys, string? warning = null) : base(settings)
    {
        LlmKeys = llmKeys;
        Warning = warning;
    }
}
