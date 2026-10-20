namespace Argos.Common.Exceptions;

/// <summary>
/// Falla de un sistema externo (Docker, archivos compartidos) que la capa de negocio decide cómo
/// informar. Si llega sin traducir a los controllers, se responde 500.
/// </summary>
public sealed class ExternalServiceException(string message, Exception? inner = null) : Exception(message, inner);
