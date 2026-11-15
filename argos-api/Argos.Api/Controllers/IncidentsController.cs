using Argos.Business.Services.Audit;
using Argos.Business.Services.Incidents;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

[Authorize]
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
        // 'tipo' es texto libre en el panel: coincidencia parcial sin distinguir mayúsculas (F-05).
        incidents.ListAsync(new IncidentFilter { Page = page, Size = size, Estado = estado, TipoContains = tipo, Search = search });

    [HttpGet("metrics")]
    public Task<IncidentMetrics> Metrics() => incidents.GetMetricsAsync();

    [HttpGet("knowledge-base")]
    public Task<PagedResult<IncidentSummary>> KnowledgeBase(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null) =>
        incidents.ListKnowledgeBaseAsync(page, size, search);

    /// <summary>"Nuevo documento" del panel: runbook manual en la base de conocimiento (solo Admin). 201 con el incidente.</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("knowledge-base")]
    public async Task<ObjectResult> CreateKnowledgeEntry(KnowledgeEntryRequest request) =>
        Created(await incidents.CreateKnowledgeEntryAsync(request, RequiredUser));

    [HttpGet("{incidentId}")]
    public Task<Incident> Get(string incidentId) => incidents.GetAsync(incidentId);

    /// <summary>Cambios desde el dashboard; se auditan con el usuario del token.</summary>
    [HttpPatch("{incidentId}")]
    public Task<Incident> Update(string incidentId, IncidentPatchRequest request) =>
        incidents.UpdateAsync(incidentId, request, RequiredUser);

    /// <summary>Borra el incidente o documento (solo Admin; p. ej. limpieza de la suite E2E). 204, o 404 si no existe.</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{incidentId}")]
    public async Task<NoContentResult> Delete(string incidentId)
    {
        await incidents.DeleteAsync(incidentId, RequiredUser);
        return NoContent();
    }

    /// <summary>Genera la documentación técnica con el LLM y la guarda en el incidente (solo Admin, como en la UI).</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("{incidentId}/document")]
    public Task<Incident> Document(string incidentId) => incidents.GenerateDocumentationAsync(incidentId, RequiredUser);

    /// <summary>Logs (Loki) y sparkline (Prometheus) del incidente.</summary>
    [HttpGet("{incidentId}/observability")]
    public Task<IncidentObservability> Observability(string incidentId) => incidents.GetObservabilityAsync(incidentId);

    [HttpGet("{incidentId}/audit")]
    public Task<IReadOnlyList<AuditEntry>> Audit(string incidentId) => audit.ListByIncidentAsync(incidentId);
}
