using Argos.Entities;

namespace Argos.Business.Services.Notifications;

public interface INotificationService
{
    /// <summary>Últimas 50, más nuevas primero.</summary>
    Task<IReadOnlyList<Notification>> ListAsync();

    /// <summary>404 si no existe.</summary>
    Task<Notification> MarkAsReadAsync(string notificationId);

    /// <summary>Cantidad de notificaciones que estaban sin leer.</summary>
    Task<int> MarkAllAsReadAsync();

    /// <summary>Crea (una sola vez por incidente) la notificación de los incidentes nuevos o críticos.</summary>
    Task ReconcileAsync(IEnumerable<IncidentSummary> incidents);

    /// <summary>Aviso de un incidente que acaba de registrar el pipeline (una sola vez por incidente).</summary>
    Task NotifyIncidentArrivalAsync(Incident incident);

    /// <summary>Aviso de que Alertmanager dio por resuelta la alerta del incidente (con su MTTR).</summary>
    Task NotifyIncidentResolvedAsync(Incident incident);

    /// <summary>Un proveedor LLM dejó de responder (<paramref name="available"/> false) o se recuperó.</summary>
    Task NotifyLlmStatusChangeAsync(LlmSelection selection, bool available, string? detail);

    /// <summary>Ningún proveedor de la cadena pudo analizar/documentar una alerta.</summary>
    Task NotifyPipelineFailureAsync(string purpose, string? alertname, string? severity, string detail);
}
