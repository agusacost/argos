using Argos.Entities;

namespace Argos.Business.Services.Observability;

/// <summary>Logs y métricas alrededor del momento de un incidente (±30 min). Degrada a vacío si Loki/Prometheus no responden.</summary>
public interface IIncidentObservabilityService
{
    Task<IncidentObservability> GetAsync(Incident incident);
}
