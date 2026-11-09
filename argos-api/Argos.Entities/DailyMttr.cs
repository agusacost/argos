namespace Argos.Entities;

/// <summary>MTTR promedio de los incidentes resueltos ese día (por <c>resolved_at</c>).</summary>
public sealed record DailyMttr(string Date, double AverageMinutes);
