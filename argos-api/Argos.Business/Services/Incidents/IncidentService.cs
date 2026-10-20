using Argos.Business.Services.Audit;
using Argos.Business.Services.Llm;
using Argos.Business.Services.Notifications;
using Argos.Business.Services.Observability;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Incidents;

public sealed class IncidentService(
    IIncidentRepository incidents,
    ISettingsRepository settings,
    IAuditService audit,
    INotificationService notifications,
    ILlmService llm,
    IIncidentObservabilityService observability,
    IIncidentMetricsCalculator metrics,
    IClock clock,
    ILogger<IncidentService> log) : IIncidentService
{
    private const string NotFoundDetail = "Incidente no encontrado";
    private const string SystemUser = "Sistema";

    public async Task<PagedResult<IncidentSummary>> ListAsync(IncidentFilter filter)
    {
        var page = await QueryAsync(filter);
        try
        {
            await notifications.ReconcileAsync(page.Data);
        }
        catch (Exception ex)
        {
            log.LogWarning("[incidents_list] reconcile error: {Error}", ex.Message);
        }
        return page;
    }

    public Task<PagedResult<IncidentSummary>> ListKnowledgeBaseAsync(int page, int size, string? search) =>
        QueryAsync(new IncidentFilter { Page = page, Size = size, Search = search, Estados = IncidentStates.Knowledge });

    public async Task<Incident> GetAsync(string incidentId) =>
        await incidents.GetByIdAsync(incidentId) ?? throw new NotFoundException(NotFoundDetail);

    public async Task<Incident> UpdateAsync(string incidentId, IncidentPatchRequest patch, CurrentUser? currentUser)
    {
        var incident = await GetAsync(incidentId);
        incident.ApplyChanges(patch.Estado, patch.ResolvedAt, patch.Documento, patch.Pasos);
        await incidents.UpdateAsync(incident);
        if (currentUser is not null)
        {
            var action = !string.IsNullOrEmpty(patch.Estado) ? $"Estado actualizado a '{patch.Estado}'" : "Campos actualizados";
            await audit.RecordAsync(incidentId, currentUser.Email, action);
        }
        return incident;
    }

    public async Task<Incident> GenerateDocumentationAsync(string incidentId, CurrentUser currentUser)
    {
        var incident = await GetAsync(incidentId);
        var selection = llm.Resolve(await settings.GetAsync());
        string markdown;
        try
        {
            markdown = await llm.GenerateIncidentDocumentAsync(selection, incident);
        }
        catch (Exception ex)
        {
            throw new UpstreamServiceException($"Error al llamar al LLM: {ex.Message}", ex);
        }

        // Se relee: la llamada al LLM tarda y el incidente pudo cambiar mientras tanto.
        var updated = await incidents.GetByIdAsync(incidentId)
                      ?? throw new NotFoundException("Incidente no encontrado tras actualización");
        updated.ApplyChanges(estado: IncidentStates.AutoDocumentado, resolvedAt: clock.UtcNowNaive(), documento: markdown);
        await incidents.UpdateAsync(updated);

        await audit.RecordAsync(incidentId, string.IsNullOrEmpty(currentUser.Email) ? SystemUser : currentUser.Email,
            "Documentación generada con LLM y aprobada");
        return updated;
    }

    public async Task<IncidentMetrics> GetMetricsAsync() => metrics.Calculate(await incidents.ListAsync());

    public async Task<IncidentObservability> GetObservabilityAsync(string incidentId) =>
        await observability.GetAsync(await GetAsync(incidentId));

    /// <summary>Filtros de Qdrant + búsqueda de texto + orden (más recientes primero) + paginado.</summary>
    private async Task<PagedResult<IncidentSummary>> QueryAsync(IncidentFilter filter)
    {
        IEnumerable<Incident> items = await incidents.FindAsync(filter);
        if (!string.IsNullOrEmpty(filter.Search))
        {
            var needle = filter.Search.ToLowerInvariant();
            items = items.Where(i =>
                i.Resumen.ToLowerInvariant().Contains(needle, StringComparison.Ordinal) ||
                i.TipoAlerta.ToLowerInvariant().Contains(needle, StringComparison.Ordinal));
        }
        var sorted = items.OrderByDescending(i => i.ReferenceDate ?? "", StringComparer.Ordinal).ToList();
        var start = Math.Max(filter.Page - 1, 0) * filter.Size;
        var data = filter.Size <= 0 || start >= sorted.Count
            ? []
            : sorted.Skip(start).Take(filter.Size).Select(i => i.ToSummary()).ToList();
        return new PagedResult<IncidentSummary>(data, sorted.Count, filter.Page, filter.Size);
    }
}
