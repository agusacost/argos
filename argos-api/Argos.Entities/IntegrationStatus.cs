namespace Argos.Entities;

/// <summary>Estado en vivo de una integración (Prometheus, Loki, Alertmanager, n8n).</summary>
public sealed record IntegrationStatus(string Key, string Name, string Status, int? HttpCode, long LatencyMs, string Detail);
