namespace Argos.Entities;

/// <summary>Golden signals de Nginx sobre una grilla temporal común (vista Métricas).</summary>
public sealed class NginxMetrics
{
    public string Range { get; init; } = "1h";
    public int StepSeconds { get; init; }
    public IReadOnlyList<long> Timestamps { get; init; } = [];
    public NginxStatusSeries StatusSeries { get; init; } = new();
    public NginxLatencySeries Latency { get; init; } = new();
    public IReadOnlyList<double?> ErrorRate { get; init; } = [];
    public IReadOnlyList<double?> RequestRate { get; init; } = [];
}
