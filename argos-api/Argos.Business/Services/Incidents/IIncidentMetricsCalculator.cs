using Argos.Entities;

namespace Argos.Business.Services.Incidents;

public interface IIncidentMetricsCalculator
{
    IncidentMetrics Calculate(IReadOnlyList<Incident> incidents);
}
