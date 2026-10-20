namespace Argos.Api.Errors;

/// <summary>Traduce las excepciones de la aplicación a status HTTP + detalle.</summary>
public interface IExceptionStatusMapper
{
    /// <summary>null si la excepción no es un error esperado (→ 500).</summary>
    ErrorMapping? Map(Exception exception);
}
