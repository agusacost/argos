namespace Argos.Entities;

/// <summary>Acción de un usuario sobre un incidente (colección <c>argos_audit</c>).</summary>
public sealed class AuditEntry : Entity
{
    public string IncidentId { get; set; } = "";
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string Timestamp { get; set; } = "";
}
