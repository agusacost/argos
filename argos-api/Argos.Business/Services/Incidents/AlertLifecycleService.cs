using Argos.Business.Services.Audit;
using Argos.Business.Services.Notifications;
using Argos.Business.Services.Settings;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Incidents;

public sealed class AlertLifecycleService(
    IIncidentRepository incidents,
    ISettingsService settings,
    INotificationService notifications,
    IAuditService audit,
    IClock clock,
    ILogger<AlertLifecycleService> log) : IAlertLifecycleService
{
    private const string PipelineSource = "alertmanager";
    private const string AlertmanagerUser = "Alertmanager";
    private const int MatchCandidates = 3;

    public async Task<AlertOccurrenceResponse> RegisterOccurrenceAsync(AlertOccurrenceRequest request)
    {
        var detectedAt = IsoDateTime.Parse(request.DetectedAt)
                         ?? throw new InvalidInputException("Campo 'detected_at': fecha ISO 8601 inválida");
        var tipo = request.TipoAlerta!;
        var existing = (await incidents.FindAsync(new IncidentFilter { Tipo = tipo }))
            .FirstOrDefault(i => i.IsOccurrenceOf(tipo, detectedAt));
        if (existing is not null)
            return new AlertOccurrenceResponse(existing.Id!, false, existing.HasSolution);

        var known = await BestDocumentedMatchAsync(request.Resumen!);
        var incident = await incidents.SaveAsync(new Incident
        {
            TipoAlerta = tipo,
            Resumen = request.Resumen!,
            Pasos = known is null ? [] : [.. known.Pasos],
            Documento = known?.Documento,
            Fecha = clock.UtcNowNaive(),
            Estado = IncidentStates.Pendiente,
            Fuente = PipelineSource,
            Severity = request.Severity,
            DetectedAt = request.DetectedAt,
        });
        await RunSafelyAsync(() => notifications.NotifyIncidentArrivalAsync(incident), incident);
        return new AlertOccurrenceResponse(incident.Id!, true, known is not null);
    }

    public async Task<AlertResolutionResponse> ResolveAsync(AlertResolutionRequest request)
    {
        var tipo = request.TipoAlerta!;
        var resolvedAt = IsoDateTime.Parse(request.ResolvedAt) ?? clock.UtcNow;
        var open = (await incidents.FindAsync(new IncidentFilter { Tipo = tipo })).Where(i => i.IsOpen).ToList();

        // Primero la ocurrencia exacta (mismo startsAt); si no, la más reciente detectada antes de la resolución
        // (incidentes registrados antes de usar startsAt como detected_at).
        var incident = IsoDateTime.Parse(request.DetectedAt) is { } detectedAt
            ? open.FirstOrDefault(i => i.IsOccurrenceOf(tipo, detectedAt))
            : null;
        incident ??= open
            .Where(i => IsoDateTime.Parse(i.DetectedAt) is { } d && d <= resolvedAt)
            .OrderByDescending(i => IsoDateTime.Parse(i.DetectedAt))
            .FirstOrDefault();
        if (incident is null)
        {
            log.LogInformation("[resolve] {Alert}: sin incidente abierto para cerrar", tipo);
            return new AlertResolutionResponse(null, null);
        }

        // Los documentados siguen en la base de conocimiento; los abiertos sin doc pasan a 'resuelto'.
        var estado = incident.Estado is IncidentStates.Nuevo or IncidentStates.Pendiente ? IncidentStates.Resuelto : null;
        incident.ApplyChanges(estado: estado, resolvedAt: IsoDateTime.Format(resolvedAt.UtcDateTime, aware: true));
        await incidents.UpdateAsync(incident);
        await audit.RecordAsync(incident.Id!, AlertmanagerUser, "Alerta resuelta automáticamente");
        await RunSafelyAsync(() => notifications.NotifyIncidentResolvedAsync(incident), incident);
        return new AlertResolutionResponse(incident.Id, incident.MttrMinutes);
    }

    private async Task<Incident?> BestDocumentedMatchAsync(string text)
    {
        var threshold = await settings.GetSimilarityThresholdAsync();
        return (await incidents.SearchSimilarAsync(text, MatchCandidates))
            // Los runbooks manuales traen pasos sin documento; los auto-documentados, documento sin pasos.
            .Where(h => h.Score > threshold && h.Incident.CountsAsKnowledge && h.Incident.HasSolution)
            .OrderByDescending(h => h.Score)
            .Select(h => h.Incident)
            .FirstOrDefault();
    }

    // El incidente ya quedó guardado: un aviso que falla no debe hacer fallar el request de n8n.
    private async Task RunSafelyAsync(Func<Task> action, Incident incident)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            log.LogWarning("[alerts] no se pudo guardar la notificación de {Alert}: {Error}", incident.TipoAlerta, ex.Message);
        }
    }
}
