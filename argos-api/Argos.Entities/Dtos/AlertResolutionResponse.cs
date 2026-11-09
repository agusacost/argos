namespace Argos.Entities.Dtos;

/// <summary><c>IncidentId</c> null si no había un incidente abierto de esa alerta.</summary>
public sealed record AlertResolutionResponse(string? IncidentId, double? MttrMinutes);
