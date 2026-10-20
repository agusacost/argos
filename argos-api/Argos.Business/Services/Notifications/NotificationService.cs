using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
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

    public async Task ReconcileAsync(IEnumerable<IncidentSummary> incidents)
    {
        var notified = (await notifications.ListAsync()).Select(n => n.IncidentId).ToHashSet();
        foreach (var incident in incidents)
        {
            if (string.IsNullOrEmpty(incident.Id) || notified.Contains(incident.Id) || !incident.RequiresNotification)
                continue;
            await notifications.SaveAsync(new Notification
            {
                IncidentId = incident.Id,
                Title = incident.TipoAlerta,
                Message = incident.Resumen,
                Severity = incident.Severity,
                Read = false,
                CreatedAt = string.IsNullOrEmpty(incident.DetectedAt) ? clock.UtcNowNaive() : incident.DetectedAt,
            });
        }
    }
}
