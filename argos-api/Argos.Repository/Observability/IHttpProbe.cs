using Argos.Entities;

namespace Argos.Repository.Observability;

/// <summary>GET de sondeo a un servicio de la red docker.</summary>
public interface IHttpProbe
{
    /// <summary>Nunca lanza: sin respuesta → <see cref="ProbeResult.StatusCode"/> null y el error en el detalle.</summary>
    Task<ProbeResult> ProbeAsync(string url);
}
