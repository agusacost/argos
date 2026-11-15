using Argos.Entities;

namespace Argos.Business.Services.Evidence;

/// <summary>Elige de dónde sale la evidencia de una alerta: Nginx, el target que nombra la alerta, o nada.</summary>
public interface IAlertEvidencePlanner
{
    /// <param name="alertname">Nombre de la regla.</param>
    /// <param name="labels">Labels de la alerta (null si no se conocen, por ejemplo desde el panel).</param>
    AlertEvidencePlan PlanFor(string alertname, IReadOnlyDictionary<string, string>? labels);
}
