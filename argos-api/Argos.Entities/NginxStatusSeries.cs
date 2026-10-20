using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>Requests por segundo según la clase del status code.</summary>
public sealed class NginxStatusSeries
{
    [JsonPropertyName("2xx")]
    public IReadOnlyList<double?> Status2xx { get; init; } = [];

    [JsonPropertyName("3xx")]
    public IReadOnlyList<double?> Status3xx { get; init; } = [];

    [JsonPropertyName("4xx")]
    public IReadOnlyList<double?> Status4xx { get; init; } = [];

    [JsonPropertyName("5xx")]
    public IReadOnlyList<double?> Status5xx { get; init; } = [];
}
