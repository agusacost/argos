namespace Argos.Common.Exceptions;

/// <summary>El recurso ya existe o hay una operación en curso (409).</summary>
public sealed class ConflictException(string detail, Exception? inner = null) : ArgosException(detail, inner);
