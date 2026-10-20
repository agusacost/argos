namespace Argos.Entities;

/// <summary>Resultado de un GET de sondeo: código HTTP (null si no hubo respuesta), cuerpo o error y latencia.</summary>
public sealed record ProbeResult(int? StatusCode, string Detail, TimeSpan Elapsed)
{
    public bool Responded => StatusCode is not null;
}
