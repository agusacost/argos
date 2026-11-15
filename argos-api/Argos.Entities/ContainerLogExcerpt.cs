namespace Argos.Entities;

/// <summary>
/// Últimas líneas relevantes de un contenedor (Loki, <c>job="docker"</c>, <c>container="…"</c>) y su estado según
/// Docker (null si no se pudo consultar).
/// </summary>
public sealed record ContainerLogExcerpt(string Container, IReadOnlyList<string> Lines, string? Status = null);
