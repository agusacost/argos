using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

/// <summary>
/// Body de <c>POST /internal/incidents/resolve</c>: Alertmanager avisó que la alerta se resolvió.
/// <c>detected_at</c> = <c>startsAt</c>, <c>resolved_at</c> = <c>endsAt</c>.
/// </summary>
public sealed class AlertResolutionRequest : IRequestDto
{
    [Required(ErrorMessage = "Campo 'tipo_alerta' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'tipo_alerta': mínimo 1 caracteres")]
    public string? TipoAlerta { get; set; }

    public string? DetectedAt { get; set; }

    public string? ResolvedAt { get; set; }
}
