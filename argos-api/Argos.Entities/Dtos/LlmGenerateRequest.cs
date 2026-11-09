using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Argos.Entities.Dtos;

/// <summary>Body de <c>POST /internal/llm/generate</c> (n8n: análisis de la alerta y documentación).</summary>
public sealed class LlmGenerateRequest : IRequestDto
{
    public const string AnalysisPurpose = "analysis";
    public const string DocumentPurpose = "document";

    [Required(ErrorMessage = "Campo 'system' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'system': mínimo 1 caracteres")]
    public string? System { get; set; }

    [Required(ErrorMessage = "Campo 'user' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'user': mínimo 1 caracteres")]
    public string? User { get; set; }

    /// <summary>"analysis" (default) o "document": solo cambia el texto del aviso si todo falla.</summary>
    public string? Purpose { get; set; }

    public string? Alertname { get; set; }
    public string? Severity { get; set; }

    /// <summary>
    /// Solo evaluación (eval/run-llm-eval.mjs): fuerza este proveedor, sin cadena de fallback ni aviso en la
    /// campana si falla. n8n no lo manda.
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>Modelo del proveedor forzado; vacío = el default del proveedor.</summary>
    public string? Model { get; set; }

    [JsonIgnore]
    public bool ForcesProvider => !string.IsNullOrWhiteSpace(Provider);
}
