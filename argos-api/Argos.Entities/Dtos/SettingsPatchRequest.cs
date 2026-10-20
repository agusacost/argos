namespace Argos.Entities.Dtos;

/// <summary>PATCH de la configuración: solo se aplican los campos informados.</summary>
public sealed class SettingsPatchRequest : IRequestDto
{
    public string? LlmProvider { get; set; }
    public string? LlmModel { get; set; }
    public double? SimilarityThreshold { get; set; }
    public bool? AutoDocEnabled { get; set; }
    public int? PollingIntervalSeconds { get; set; }
    public List<string>? AlertRecipients { get; set; }
}
