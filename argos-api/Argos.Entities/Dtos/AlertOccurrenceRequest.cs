using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

/// <summary>
/// Body de <c>POST /internal/incidents/occurrence</c>: una alerta que ya tiene documentación en la base
/// (n8n, rama "conocida"). <c>detected_at</c> es el <c>startsAt</c> de Alertmanager.
/// </summary>
public sealed class AlertOccurrenceRequest : IRequestDto
{
    [Required(ErrorMessage = "Campo 'tipo_alerta' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'tipo_alerta': mínimo 1 caracteres")]
    public string? TipoAlerta { get; set; }

    [Required(ErrorMessage = "Campo 'resumen' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'resumen': mínimo 1 caracteres")]
    public string? Resumen { get; set; }

    [Required(ErrorMessage = "Campo 'detected_at' requerido")]
    public string? DetectedAt { get; set; }

    public string? Severity { get; set; }
}
