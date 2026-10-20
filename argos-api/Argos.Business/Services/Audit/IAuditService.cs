using Argos.Entities;

namespace Argos.Business.Services.Audit;

public interface IAuditService
{
    /// <summary>Registra la acción; si falla solo se loguea (la auditoría no corta la operación).</summary>
    Task RecordAsync(string incidentId, string user, string action);

    /// <summary>Historial de un incidente, del más viejo al más nuevo.</summary>
    Task<IReadOnlyList<AuditEntry>> ListByIncidentAsync(string incidentId);

    /// <summary>Actividad reciente de toda la plataforma (más nuevas primero).</summary>
    Task<IReadOnlyList<AuditEntry>> ListRecentAsync(int limit);
}
