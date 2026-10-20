namespace Argos.Common.Exceptions;

/// <summary>Falta autenticación o el token no es válido (401).</summary>
public sealed class UnauthorizedException(string detail, Exception? inner = null) : ArgosException(detail, inner);
