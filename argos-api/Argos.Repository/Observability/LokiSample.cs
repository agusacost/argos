namespace Argos.Repository.Observability;

/// <summary>Una serie de una query instantánea de Loki (vector): labels y valor.</summary>
public sealed record LokiSample(IReadOnlyDictionary<string, string> Labels, double Value);
