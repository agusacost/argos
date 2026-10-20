namespace Argos.Entities.Dtos;

/// <summary>Error en el formato <c>{"detail": "..."}</c>, el que lee el dashboard.</summary>
public sealed record ErrorResponse(string Detail);
