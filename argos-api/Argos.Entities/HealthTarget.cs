namespace Argos.Entities;

/// <summary>Servicio a sondear por HTTP; <see cref="PrometheusJob"/> se usa para la sparkline de <c>up</c>.</summary>
public sealed record HealthTarget(string Name, string Component, string? PrometheusJob, string Url);
