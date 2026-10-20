namespace Argos.Entities.Dtos;

/// <summary>PATCH de incidente desde el dashboard: solo se aplican los campos informados.</summary>
public sealed class IncidentPatchRequest : IRequestDto
{
    public string? Estado { get; set; }
    public string? ResolvedAt { get; set; }
    public string? Documento { get; set; }
    public List<string>? Pasos { get; set; }
}
