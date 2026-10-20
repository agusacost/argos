using System.Text.Json;
using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>
/// Configuración editable desde el dashboard (documento único de <c>argos_settings</c>).
/// Los inicializadores son los defaults: un campo ausente en el documento guardado conserva su default.
/// </summary>
public class AppSettings
{
    // Claves que pueden seguir en documentos viejos y ya no se exponen (integraciones = estado en vivo).
    private static readonly string[] ObsoleteKeys = ["id", "integrations"];

    public string LlmProvider { get; set; } = "gemini";
    public string LlmModel { get; set; } = "gemini-flash-latest";
    public double SimilarityThreshold { get; set; } = 0.63;
    public bool AutoDocEnabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 20;

    /// <summary>Pool de destinatarios del email de alertas (n8n). Vacío → fallback a ALERT_RECIPIENT_EMAIL.</summary>
    public List<string> AlertRecipients { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }

    public AppSettings()
    {
    }

    protected AppSettings(AppSettings source)
    {
        LlmProvider = source.LlmProvider;
        LlmModel = source.LlmModel;
        SimilarityThreshold = source.SimilarityThreshold;
        AutoDocEnabled = source.AutoDocEnabled;
        PollingIntervalSeconds = source.PollingIntervalSeconds;
        AlertRecipients = [.. source.AlertRecipients];
        ExtraFields = source.ExtraFields is null ? null : new Dictionary<string, JsonElement>(source.ExtraFields);
    }

    public AppSettings Copy() => new(this);

    public void RemoveObsoleteFields()
    {
        if (ExtraFields is null) return;
        foreach (var key in ObsoleteKeys) ExtraFields.Remove(key);
    }

    /// <summary>Aplica los valores informados (los null se ignoran).</summary>
    public void ApplyPatch(string? llmProvider, string? llmModel, double? similarityThreshold, bool? autoDocEnabled,
        int? pollingIntervalSeconds, IReadOnlyList<string>? alertRecipients)
    {
        if (llmProvider is not null) LlmProvider = llmProvider;
        if (llmModel is not null) LlmModel = llmModel;
        if (similarityThreshold is not null) SimilarityThreshold = similarityThreshold.Value;
        if (autoDocEnabled is not null) AutoDocEnabled = autoDocEnabled.Value;
        if (pollingIntervalSeconds is not null) PollingIntervalSeconds = pollingIntervalSeconds.Value;
        if (alertRecipients is not null) AlertRecipients = [.. alertRecipients];
    }

    public bool UsesSameLlmAs(AppSettings other) => LlmProvider == other.LlmProvider && LlmModel == other.LlmModel;
}
