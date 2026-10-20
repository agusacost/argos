using Argos.Business.Services.Audit;
using Argos.Business.Services.Incidents;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

[Route("incidents")]
public sealed class IncidentsController(IIncidentService incidents, IAuditService audit) : ArgosControllerBase
{
    [HttpGet]
    public Task<PagedResult<IncidentSummary>> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? estado = null,
        [FromQuery] string? tipo = null,
        [FromQuery] string? search = null) =>
        incidents.ListAsync(new IncidentFilter { Page = page, Size = size, Estado = estado, Tipo = tipo, Search = search });

    [HttpGet("metrics")]
    public Task<IncidentMetrics> Metrics() => incidents.GetMetricsAsync();

    [HttpGet("knowledge-base")]
    public Task<PagedResult<IncidentSummary>> KnowledgeBase(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null) =>
        incidents.ListKnowledgeBaseAsync(page, size, search);

    [HttpGet("{incidentId}")]
    public Task<Incident> Get(string incidentId) => incidents.GetAsync(incidentId);

    /// <summary>Cambios desde el dashboard; se auditan si el request trae un token válido.</summary>
    [HttpPatch("{incidentId}")]
    public Task<Incident> Update(string incidentId, IncidentPatchRequest request) =>
        incidents.UpdateAsync(incidentId, request, AuthenticatedUser);

    /// <summary>Genera la documentación técnica con el LLM y la guarda en el incidente.</summary>
    [Authorize]
    [HttpPost("{incidentId}/document")]
    public Task<Incident> Document(string incidentId) => incidents.GenerateDocumentationAsync(incidentId, RequiredUser);

    /// <summary>Logs (Loki) y sparkline (Prometheus) del incidente.</summary>
    [HttpGet("{incidentId}/observability")]
    public Task<IncidentObservability> Observability(string incidentId) => incidents.GetObservabilityAsync(incidentId);

    [HttpGet("{incidentId}/audit")]
    public Task<IReadOnlyList<AuditEntry>> Audit(string incidentId) => audit.ListByIncidentAsync(incidentId);
}
