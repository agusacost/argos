namespace Argos.Entities;

/// <summary>Métricas agregadas de la colección de incidentes para la vista Dashboard.</summary>
public sealed class IncidentMetrics
{
    public int TotalIncidents { get; init; }
    public Dictionary<string, int> ByStatus { get; init; } = [];
    public Dictionary<string, int> ByAlertType { get; init; } = [];
    public double? MttrAverageMinutes { get; init; }
    public List<DailyIncidentCount> IncidentsPerDay { get; init; } = [];

    /// <summary><c>stable</c>, <c>degrading</c> o <c>improving</c>.</summary>
    public string RecentTrend { get; init; } = "stable";
}
