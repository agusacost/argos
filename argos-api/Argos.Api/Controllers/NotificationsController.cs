using Argos.Business.Services.Audit;
using Argos.Business.Services.Notifications;
using Argos.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Campana de notificaciones y actividad reciente del Dashboard.</summary>
public sealed class NotificationsController(INotificationService notifications, IAuditService audit) : ArgosControllerBase
{
    private const int RecentActivityLimit = 10;

    [Authorize]
    [HttpGet("notifications")]
    public Task<IReadOnlyList<Notification>> List() => notifications.ListAsync();

    [Authorize]
    [HttpPost("notifications/{notificationId}/read")]
    public Task<Notification> MarkAsRead(string notificationId) => notifications.MarkAsReadAsync(notificationId);

    [HttpGet("activity")]
    public Task<IReadOnlyList<AuditEntry>> Activity() => audit.ListRecentAsync(RecentActivityLimit);
}
