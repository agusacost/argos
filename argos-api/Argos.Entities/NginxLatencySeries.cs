namespace Argos.Entities;

/// <summary>Latencia upstream en segundos (null donde no hubo requests).</summary>
public sealed class NginxLatencySeries
{
    public IReadOnlyList<double?> P50 { get; init; } = [];
    public IReadOnlyList<double?> P95 { get; init; } = [];
    public IReadOnlyList<double?> P99 { get; init; } = [];
}
