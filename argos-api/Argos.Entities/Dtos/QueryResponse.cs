namespace Argos.Entities.Dtos;

public sealed record QueryResponse(string Query, double Threshold, bool IsDocumented, IReadOnlyList<IncidentMatch> Results);
