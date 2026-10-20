using Argos.Entities;

namespace Argos.Repository.Repositories;

public interface IAuditRepository : IRepository<AuditEntry>
{
    Task<IReadOnlyList<AuditEntry>> ListByIncidentAsync(string incidentId);
}
