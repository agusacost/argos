namespace Argos.Entities;

/// <summary>Métricas agregadas de la colección de incidentes para la vista Dashboard.</summary>
public sealed class IncidentMetrics
{
    public int TotalIncidents { get; init; }
    public Dictionary<string, int> ByStatus { get; init; } = [];
    public Dictionary<string, int> ByAlertType { get; init; } = [];
    public double? MttrAverageMinutes { get; init; }
    public List<DailyIncidentCount> IncidentsPerDay { get; init; } = [];

    /// <summary>Tendencia del VOLUMEN de incidentes: <c>stable</c>, <c>degrading</c> o <c>improving</c>.</summary>
    public string RecentTrend { get; init; } = "stable";

    /// <summary>
    /// Tendencia del MTTR (últimos 7 días contra los 7 anteriores): <c>improving</c> (baja), <c>degrading</c>
    /// (sube), <c>stable</c> o <c>no_data</c> si falta alguna de las dos ventanas.
    /// </summary>
    public string MttrTrend { get; init; } = "no_data";

    /// <summary>MTTR promedio por día de resolución, últimos 14 días.</summary>
    public List<DailyMttr> MttrPerDay { get; init; } = [];
}
