using Argos.Business.Services.Audit;
using Argos.Business.Services.Evidence;
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
    IAlertEvidenceService evidence,
    IIncidentMetricsCalculator metrics,
    IClock clock,
    ILogger<IncidentService> log) : IIncidentService
{
    private const string NotFoundDetail = "Incidente no encontrado";
    private const string SystemUser = "Sistema";
    private const string ManualSource = "manual";

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

    public async Task<Incident> UpdateAsync(string incidentId, IncidentPatchRequest patch, CurrentUser currentUser)
    {
        var incident = await GetAsync(incidentId);
        incident.ApplyChanges(patch.Estado, patch.ResolvedAt, patch.Documento, patch.Pasos);
        await incidents.UpdateAsync(incident);
        var action = !string.IsNullOrEmpty(patch.Estado) ? $"Estado actualizado a '{patch.Estado}'" : "Campos actualizados";
        await audit.RecordAsync(incidentId, currentUser.Email, action);
        return incident;
    }

    public async Task<Incident> CreateKnowledgeEntryAsync(KnowledgeEntryRequest request, CurrentUser currentUser)
    {
        var incident = new Incident
        {
            TipoAlerta = request.TipoAlerta!.Trim(),
            Resumen = request.Resumen!.Trim(),
            Pasos = [.. request.CleanPasos],
            Documento = request.CleanDocumento,
            Severity = request.Severity?.Trim().ToLowerInvariant(),
            Fecha = clock.UtcNowNaive(),
            Estado = IncidentStates.DocumentadoPreviamente,
            Fuente = ManualSource,
        };
        await incidents.EnsureCollectionAsync();
        // Mismo tipo + resumen = mismo id: guardarlo pisaría el runbook existente sin aviso.
        if (await incidents.GetExistingAsync(incident) is not null)
            throw new ConflictException("Ya existe un documento con ese tipo de alerta y resumen");
        var saved = await incidents.SaveAsync(incident);
        await audit.RecordAsync(saved.Id!, ActorOf(currentUser), "Documento de la base de conocimiento creado manualmente");
        return saved;
    }

    public async Task DeleteAsync(string incidentId, CurrentUser currentUser)
    {
        var incident = await GetAsync(incidentId);
        if (!await incidents.DeleteAsync(incidentId)) throw new NotFoundException(NotFoundDetail);
        await audit.RecordAsync(incidentId, ActorOf(currentUser),
            $"Incidente eliminado ({incident.TipoAlerta}, estado '{incident.Estado}')");
    }

    public async Task<Incident> GenerateDocumentationAsync(string incidentId, CurrentUser currentUser)
    {
        var incident = await GetAsync(incidentId);
        // Misma evidencia de logs que usa n8n, en la ventana del incidente (vacía si Loki no responde).
        var logs = await evidence.CollectAsync(incident.TipoAlerta, null, IsoDateTime.Parse(incident.ReferenceDate),
            IsoDateTime.Parse(incident.ResolvedAt));
        var generation = await llm.GenerateIncidentDocumentAsync(await settings.GetAsync(), incident, logs.Text);

        // Se relee: la llamada al LLM tarda y el incidente pudo cambiar mientras tanto.
        var updated = await incidents.GetByIdAsync(incidentId)
                      ?? throw new NotFoundException("Incidente no encontrado tras actualización");
        updated.ApplyChanges(estado: IncidentStates.AutoDocumentado, resolvedAt: clock.UtcNowNaive(), documento: generation.Text);
        await incidents.UpdateAsync(updated);

        await audit.RecordAsync(incidentId, ActorOf(currentUser),
            $"Documentación generada con LLM ({generation.Provider}/{generation.Model}) y aprobada");
        return updated;
    }

    public async Task<IncidentMetrics> GetMetricsAsync() => metrics.Calculate(await incidents.ListAsync());

    public async Task<IncidentObservability> GetObservabilityAsync(string incidentId) =>
        await observability.GetAsync(await GetAsync(incidentId));

    private static string ActorOf(CurrentUser user) => string.IsNullOrEmpty(user.Email) ? SystemUser : user.Email;

    /// <summary>Filtros de Qdrant + búsqueda de texto + orden (más recientes primero) + paginado.</summary>
    private async Task<PagedResult<IncidentSummary>> QueryAsync(IncidentFilter filter)
    {
        var items = (await incidents.FindAsync(filter)).Where(filter.MatchesText);
        var sorted = items.OrderByDescending(i => i.ReferenceDate ?? "", StringComparer.Ordinal).ToList();
        var start = Math.Max(filter.Page - 1, 0) * filter.Size;
        var data = filter.Size <= 0 || start >= sorted.Count
            ? []
            : sorted.Skip(start).Take(filter.Size).Select(i => i.ToSummary()).ToList();
        return new PagedResult<IncidentSummary>(data, sorted.Count, filter.Page, filter.Size);
    }
}
