using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

/// <summary>Body de <c>POST /internal/evidence</c> (n8n, nodo 'Get Evidence').</summary>
public sealed class AlertEvidenceRequest : IRequestDto
{
    [Required(ErrorMessage = "Campo 'alertname' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'alertname': mínimo 1 caracteres")]
    public string? Alertname { get; set; }

    /// <summary><c>startsAt</c> de Alertmanager (ISO 8601); vacío o inválido = ahora.</summary>
    public string? StartsAt { get; set; }

    /// <summary><c>endsAt</c> si la alerta ya se resolvió; si no, la ventana termina ahora.</summary>
    public string? EndsAt { get; set; }

    /// <summary>
    /// Labels de la alerta. <c>evidence</c> (declarado en la regla) elige la fuente: <c>nginx</c> o <c>target</c>;
    /// <c>instance</c> es el host del target. Sin labels se usa un criterio de respaldo (ver AlertEvidencePlanner).
    /// </summary>
    public Dictionary<string, string>? Labels { get; set; }
}
