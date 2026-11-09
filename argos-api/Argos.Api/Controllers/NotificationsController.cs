using Argos.Business.Services.Audit;
using Argos.Business.Services.Notifications;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Campana de notificaciones y actividad reciente del Dashboard.</summary>
[Authorize]
public sealed class NotificationsController(INotificationService notifications, IAuditService audit) : ArgosControllerBase
{
    private const int RecentActivityLimit = 10;

    [HttpGet("notifications")]
    public Task<IReadOnlyList<Notification>> List() => notifications.ListAsync();

    [HttpPost("notifications/read-all")]
    public async Task<MarkAllReadResponse> MarkAllAsRead() => new(await notifications.MarkAllAsReadAsync());

    [HttpPost("notifications/{notificationId}/read")]
    public Task<Notification> MarkAsRead(string notificationId) => notifications.MarkAsReadAsync(notificationId);

    [HttpGet("activity")]
    public Task<IReadOnlyList<AuditEntry>> Activity() => audit.ListRecentAsync(RecentActivityLimit);
}
