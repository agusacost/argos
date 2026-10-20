namespace Argos.Common.Exceptions;

/// <summary>Qdrant no responde (503).</summary>
public sealed class DataStoreUnavailableException(string detail, Exception? inner = null) : ArgosException(detail, inner);
