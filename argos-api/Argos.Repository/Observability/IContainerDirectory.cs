using Argos.Entities;

namespace Argos.Repository.Observability;

/// <summary>
/// Traduce hosts que reporta la observabilidad (IP del upstream en los logs de Nginx, nombre del <c>instance</c> de una
/// alerta) al contenedor que los atiende, consultando Docker. Nunca falla: sin Docker devuelve una lista vacía.
/// </summary>
public interface IContainerDirectory
{
    Task<IReadOnlyList<ResolvedContainer>> ResolveAsync(IReadOnlyCollection<string> hosts);
}
