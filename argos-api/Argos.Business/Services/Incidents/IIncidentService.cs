using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Incidents;

public interface IIncidentService
{
    /// <summary>Listado paginado (sin documento) y, de paso, notificaciones de los incidentes nuevos o críticos.</summary>
    Task<PagedResult<IncidentSummary>> ListAsync(IncidentFilter filter);

    /// <summary>Runbooks manuales y documentos generados por el LLM.</summary>
    Task<PagedResult<IncidentSummary>> ListKnowledgeBaseAsync(int page, int size, string? search);

    /// <summary>404 si no existe.</summary>
    Task<Incident> GetAsync(string incidentId);

    /// <summary>Aplica el PATCH del dashboard y lo audita con el usuario autenticado.</summary>
    Task<Incident> UpdateAsync(string incidentId, IncidentPatchRequest patch, CurrentUser currentUser);

    /// <summary>
    /// Alta manual de un runbook (estado documentado_previamente, fuente manual) desde el panel, auditada.
    /// 409 si ya existe uno con el mismo tipo de alerta y resumen.
    /// </summary>
    Task<Incident> CreateKnowledgeEntryAsync(KnowledgeEntryRequest request, CurrentUser currentUser);

    /// <summary>Borra el incidente (sale también del RAG) y lo audita; 404 si no existe.</summary>
    Task DeleteAsync(string incidentId, CurrentUser currentUser);

    /// <summary>Genera la documentación con el LLM, la guarda (estado auto_documentado) y lo audita.</summary>
    Task<Incident> GenerateDocumentationAsync(string incidentId, CurrentUser currentUser);

    Task<IncidentMetrics> GetMetricsAsync();

    /// <summary>Logs (Loki) y sparkline (Prometheus) del incidente.</summary>
    Task<IncidentObservability> GetObservabilityAsync(string incidentId);
}
