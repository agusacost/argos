using Argos.Entities;

namespace Argos.Business.Seeding;

public interface IDemoIncidentCatalog
{
    IReadOnlyList<Incident> GetIncidents();
}
