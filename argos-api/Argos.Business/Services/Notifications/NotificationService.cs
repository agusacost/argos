using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;

namespace Argos.Business.Services.Notifications;

public sealed class NotificationService(INotificationRepository notifications, IClock clock) : INotificationService
{
    private const int MaxListed = 50;

    public async Task<IReadOnlyList<Notification>> ListAsync() =>
        (await notifications.ListAsync())
            .OrderByDescending(n => n.CreatedAt, StringComparer.Ordinal)
            .Take(MaxListed)
            .ToList();

    public async Task<Notification> MarkAsReadAsync(string notificationId)
    {
        var notification = await notifications.GetByIdAsync(notificationId)
                           ?? throw new NotFoundException("Notificación no encontrada");
        notification.MarkAsRead();
        await notifications.UpdateAsync(notification);
        return notification;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var unread = (await notifications.ListAsync()).Where(n => !n.Read).ToList();
        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            await notifications.UpdateAsync(notification);
        }
        return unread.Count;
    }

    public async Task ReconcileAsync(IEnumerable<IncidentSummary> incidents)
    {
        var notified = await NotifiedIncidentIdsAsync();
        foreach (var incident in incidents)
        {
            if (string.IsNullOrEmpty(incident.Id) || notified.Contains(incident.Id) || !incident.RequiresNotification)
                continue;
            await notifications.SaveAsync(IncidentNotification(incident, incident.Resumen,
                string.IsNullOrEmpty(incident.DetectedAt) ? clock.UtcNowAware() : incident.DetectedAt));
        }
    }

    public async Task NotifyIncidentArrivalAsync(Incident incident)
    {
        if (string.IsNullOrEmpty(incident.Id) || (await NotifiedIncidentIdsAsync()).Contains(incident.Id)) return;
        var prefix = incident.Estado switch
        {
            IncidentStates.AutoDocumentado => "Incidente nuevo auto-documentado por IA",
            IncidentStates.Nuevo => "Incidente nuevo pendiente de documentar",
            _ when incident.HasSolution => "Incidente conocido, con solución en la base de conocimiento",
            _ => "Incidente registrado",
        };
        await notifications.SaveAsync(IncidentNotification(incident, $"{prefix}: {incident.Resumen}", clock.UtcNowAware()));
    }

    public Task NotifyIncidentResolvedAsync(Incident incident)
    {
        var duration = incident.MttrMinutes is { } mttr ? $" en {mttr:0.#} min" : "";
        var notification = IncidentNotification(incident, $"La alerta se resolvió{duration} (Alertmanager).", clock.UtcNowAware());
        notification.Title = $"Resuelto: {incident.TipoAlerta}";
        notification.Severity = "info";
        return notifications.SaveAsync(notification);
    }

    public Task NotifyLlmStatusChangeAsync(LlmSelection selection, bool available, string? detail) =>
        notifications.SaveAsync(new Notification
        {
            Kind = NotificationKinds.LlmHealth,
            Title = available ? $"IA: {selection.Provider} se recuperó" : $"IA: {selection.Provider} no disponible",
            Message = available
                ? $"{selection.Model} vuelve a responder."
                : $"{selection.Model} — {detail}. Las alertas se derivan al siguiente proveedor de respaldo.",
            Severity = available ? "info" : "warning",
            CreatedAt = clock.UtcNowAware(),
        });

    public Task NotifyPipelineFailureAsync(string purpose, string? alertname, string? severity, string detail)
    {
        var task = purpose == LlmGenerateRequest.DocumentPurpose ? "Auto-documentación fallida" : "Análisis IA fallido";
        return notifications.SaveAsync(new Notification
        {
            Kind = NotificationKinds.PipelineError,
            Title = $"{task}: {(string.IsNullOrEmpty(alertname) ? "alerta" : alertname)}",
            Message = $"Ningún proveedor LLM respondió; el incidente queda para documentar a mano. {detail}",
            Severity = severity == "critical" ? "critical" : "warning",
            CreatedAt = clock.UtcNowAware(),
        });
    }

    private async Task<HashSet<string>> NotifiedIncidentIdsAsync() =>
        (await notifications.ListAsync())
            .Select(n => n.IncidentId)
            .OfType<string>()
            .ToHashSet();

    private static Notification IncidentNotification(IncidentSummary incident, string message, string createdAt) => new()
    {
        Kind = NotificationKinds.Incident,
        IncidentId = incident.Id,
        Title = incident.TipoAlerta,
        Message = message,
        Severity = incident.Severity,
        Read = false,
        CreatedAt = createdAt,
    };
}
