namespace Argos.Common.Exceptions;

/// <summary>El body o los parámetros no cumplen el contrato (422).</summary>
public sealed class InvalidInputException(string detail, Exception? inner = null) : ArgosException(detail, inner);
