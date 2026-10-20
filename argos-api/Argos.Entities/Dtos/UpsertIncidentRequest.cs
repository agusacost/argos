using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

/// <summary>Body de <c>POST /upsert</c>: incidente documentado por n8n o cargado a mano.</summary>
public sealed class UpsertIncidentRequest : IRequestDto
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'tipo_alerta' requerido")]
    public string? TipoAlerta { get; set; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'resumen' requerido")]
    public string? Resumen { get; set; }

    public List<string>? Pasos { get; set; }
    public string? Fecha { get; set; }
    public string? Estado { get; set; }
    public string? Fuente { get; set; }
    public string? Documento { get; set; }
    public string? Severity { get; set; }
    public string? DetectedAt { get; set; }
    public string? ResolvedAt { get; set; }
    public double? MttrMinutes { get; set; }
}
