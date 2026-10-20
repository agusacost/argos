namespace Argos.Common.Exceptions;

/// <summary>El recurso pedido no existe (404).</summary>
public sealed class NotFoundException(string detail, Exception? inner = null) : ArgosException(detail, inner);
