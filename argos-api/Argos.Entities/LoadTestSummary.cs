namespace Argos.Entities;

/// <summary>Métricas principales del <c>--summary-export</c> de k6 (redondeadas a 2 decimales).</summary>
public sealed class LoadTestSummary
{
    public double? Reqs { get; set; }
    public double? ThroughputRps { get; set; }
    public double? P95Ms { get; set; }
    public double? AvgMs { get; set; }
    public double? ErrorRate { get; set; }
    public double? ChecksRate { get; set; }
}
