namespace Argos.Common.Exceptions;

/// <summary>Un servicio externo (LLM) falló al responder (502).</summary>
public sealed class UpstreamServiceException(string detail, Exception? inner = null) : ArgosException(detail, inner);
