namespace Argos.Entities;

/// <summary>Logs de Loki y serie de Prometheus en la ventana de un incidente.</summary>
public sealed record IncidentObservability(IReadOnlyList<string> Logs, IReadOnlyList<double> Sparkline);
