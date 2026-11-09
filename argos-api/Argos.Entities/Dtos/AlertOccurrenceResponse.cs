namespace Argos.Entities.Dtos;

/// <summary><c>Created</c> false si esa ocurrencia ya estaba registrada (repetición de Alertmanager).</summary>
public sealed record AlertOccurrenceResponse(string Id, bool Created, bool DocumentationCopied);
