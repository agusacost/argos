namespace Argos.Entities.Dtos;

/// <summary>
/// Respuesta de <c>POST /query</c>. <c>match_reason</c> ("similarity" | "alert_name" | "none") y
/// <c>name_match_min_score</c> se agregaron sin cambiar los campos anteriores (compatible hacia atrás).
/// </summary>
public sealed record QueryResponse(
    string Query,
    double Threshold,
    bool IsDocumented,
    IReadOnlyList<IncidentMatch> Results,
    string MatchReason,
    double NameMatchMinScore);
