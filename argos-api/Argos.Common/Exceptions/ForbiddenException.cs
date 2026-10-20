namespace Argos.Common.Exceptions;

/// <summary>El usuario no tiene el rol requerido (403).</summary>
public sealed class ForbiddenException(string detail, Exception? inner = null) : ArgosException(detail, inner);
