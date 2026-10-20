namespace Argos.Entities;

/// <summary>Aviso de la campana del dashboard (colección <c>argos_notifications</c>).</summary>
public sealed class Notification : Entity
{
    public string IncidentId { get; set; } = "";
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Severity { get; set; }
    public bool Read { get; set; }
    public string CreatedAt { get; set; } = "";

    public void MarkAsRead() => Read = true;
}
