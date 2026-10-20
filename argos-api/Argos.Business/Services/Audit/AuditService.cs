using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Audit;

public sealed class AuditService(IAuditRepository audit, IClock clock, ILogger<AuditService> log) : IAuditService
{
    public async Task RecordAsync(string incidentId, string user, string action)
    {
        try
        {
            await audit.SaveAsync(new AuditEntry
            {
                IncidentId = incidentId,
                User = user,
                Action = action,
                Timestamp = clock.UtcNowNaive(),
            });
        }
        catch (Exception ex)
        {
            log.LogWarning("[audit] error: {Error}", ex.Message);
        }
    }

    public async Task<IReadOnlyList<AuditEntry>> ListByIncidentAsync(string incidentId) =>
        (await audit.ListByIncidentAsync(incidentId)).OrderBy(e => e.Timestamp, StringComparer.Ordinal).ToList();

    public async Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int limit) =>
        (await audit.ListAsync()).OrderByDescending(e => e.Timestamp, StringComparer.Ordinal).Take(limit).ToList();
}
