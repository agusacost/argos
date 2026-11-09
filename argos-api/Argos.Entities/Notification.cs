namespace Argos.Entities;

/// <summary>Aviso de la campana del dashboard (colección <c>argos_notifications</c>).</summary>
public sealed class Notification : Entity
{
    /// <summary>Uno de <see cref="NotificationKinds"/>; los documentos viejos sin el campo son de incidente.</summary>
    public string Kind { get; set; } = NotificationKinds.Incident;

    /// <summary>Incidente al que lleva el aviso (null en los avisos de salud de la IA).</summary>
    public string? IncidentId { get; set; }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Severity { get; set; }
    public bool Read { get; set; }
    public string CreatedAt { get; set; } = "";

    public void MarkAsRead() => Read = true;
}
