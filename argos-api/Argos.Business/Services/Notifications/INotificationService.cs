using Argos.Entities;

namespace Argos.Business.Services.Notifications;

public interface INotificationService
{
    /// <summary>Últimas 50, más nuevas primero.</summary>
    Task<IReadOnlyList<Notification>> ListAsync();

    /// <summary>404 si no existe.</summary>
    Task<Notification> MarkAsReadAsync(string notificationId);

    /// <summary>Crea (una sola vez por incidente) la notificación de los incidentes nuevos o críticos.</summary>
    Task ReconcileAsync(IEnumerable<IncidentSummary> incidents);
}
