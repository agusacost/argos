namespace Argos.Common.Exceptions;

/// <summary>Pedido inválido para el estado actual (400).</summary>
public sealed class BadRequestException(string detail, Exception? inner = null) : ArgosException(detail, inner);
