using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Argos.Entities.Dtos;

/// <summary>
/// Body de <c>POST /incidents/knowledge-base</c> (panel, "Nuevo documento"): runbook manual. Requiere
/// <c>tipo_alerta</c>, <c>resumen</c> y al menos pasos o documento; <c>severity</c> es "warning" o "critical".
/// Cualquier incumplimiento responde 422 con <c>{"detail": "..."}</c>.
/// </summary>
public sealed class KnowledgeEntryRequest : IRequestDto, IValidatableObject
{
    public static readonly IReadOnlyList<string> Severities = ["warning", "critical"];

    [Required(ErrorMessage = "Campo 'tipo_alerta' requerido")]
    [MaxLength(200, ErrorMessage = "Campo 'tipo_alerta': máximo 200 caracteres")]
    public string? TipoAlerta { get; set; }

    [Required(ErrorMessage = "Campo 'resumen' requerido")]
    [MaxLength(2000, ErrorMessage = "Campo 'resumen': máximo 2000 caracteres")]
    public string? Resumen { get; set; }

    public List<string?>? Pasos { get; set; }

    /// <summary>Markdown opcional.</summary>
    public string? Documento { get; set; }

    public string? Severity { get; set; }

    /// <summary>Pasos sin vacíos ni espacios sobrantes.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> CleanPasos =>
        (Pasos ?? []).Select(p => (p ?? "").Trim()).Where(p => p.Length > 0).ToList();

    /// <summary>Documento sin espacios sobrantes, o null si vino vacío.</summary>
    [JsonIgnore]
    public string? CleanDocumento => string.IsNullOrWhiteSpace(Documento) ? null : Documento.Trim();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CleanPasos.Count == 0 && CleanDocumento is null)
            yield return new ValidationResult("Hay que informar al menos un paso de resolución o el documento",
                [nameof(Pasos), nameof(Documento)]);
        if (Severity is not null && !Severities.Contains(Severity.Trim().ToLowerInvariant()))
            yield return new ValidationResult("Campo 'severity': debe ser 'warning' o 'critical'", [nameof(Severity)]);
    }
}
