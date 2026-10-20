namespace Argos.Entities.Dtos;

/// <summary>Pool de destinatarios del email de alertas (n8n, nodo 'Get Recipients').</summary>
public sealed record RecipientsResponse(IReadOnlyList<string> Recipients, int Count);
