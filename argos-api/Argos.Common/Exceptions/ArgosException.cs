namespace Argos.Common.Exceptions;

/// <summary>
/// Error esperado de la aplicación. La capa de controllers lo traduce a un status HTTP y a
/// <c>{"detail": "..."}</c>, el formato de error que consume el dashboard.
/// </summary>
public abstract class ArgosException(string detail, Exception? inner = null) : Exception(detail, inner)
{
    public string Detail { get; } = detail;
}
