namespace Argos.Common.Configuration;

/// <summary>Evidencia de logs (Loki) que acompaña al prompt del análisis y de la documentación.</summary>
public interface IEvidenceConfig
{
    /// <summary>ALERT_EVIDENCE_ENABLED (default true). Apagado, la evidencia sale vacía y el flujo es el de antes.</summary>
    bool AlertEvidenceEnabled { get; }

    /// <summary>
    /// DOCKER_HOST (default unix:///var/run/docker.sock). Se usa para traducir el upstream que reporta Nginx (o el
    /// <c>instance</c> de la alerta) al nombre del contenedor; sin Docker, la evidencia queda solo en Nginx.
    /// </summary>
    string DockerHost { get; }
}
