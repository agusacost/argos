using Argos.Entities;

namespace Argos.Business.Services.Evidence;

/// <summary>Arma la muestra acotada de evidencia (presupuesto fijo de líneas) y la sección del prompt.</summary>
public interface IAlertEvidenceFormatter
{
    /// <param name="alertname">Tipo de alerta.</param>
    /// <param name="start">Inicio de la ventana.</param>
    /// <param name="end">Fin de la ventana.</param>
    /// <param name="statusCounts">Distribución status / upstream_status del access.log.</param>
    /// <param name="nginxErrors">Líneas crudas del error.log, de la más nueva a la más vieja.</param>
    /// <param name="containerLines">Líneas crudas por contenedor, de la más nueva a la más vieja, con su estado.</param>
    /// <param name="upstreams">Hosts que Nginx reportó como upstream de los errores.</param>
    AlertEvidence Build(string alertname, DateTimeOffset start, DateTimeOffset end,
        IReadOnlyList<NginxStatusCount> statusCounts,
        IReadOnlyList<string> nginxErrors,
        IReadOnlyList<ContainerLogExcerpt> containerLines,
        IReadOnlyList<string>? upstreams = null);
}
