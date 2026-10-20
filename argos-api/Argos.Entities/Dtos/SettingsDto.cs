using System.Text.Json.Serialization;

namespace Argos.Entities.Dtos;

/// <summary>Configuración + estado de las keys LLM (+ advertencia de la validación del modelo, si hubo).</summary>
public sealed class SettingsDto : AppSettings
{
    public LlmKeysDto LlmKeys { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Warning { get; }

    public SettingsDto(AppSettings settings, LlmKeysDto llmKeys, string? warning = null) : base(settings)
    {
        LlmKeys = llmKeys;
        Warning = warning;
    }
}
