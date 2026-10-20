namespace Argos.Entities;

/// <summary>Estado de un servicio de la plataforma ("Salud de Servicios").</summary>
public sealed record ServiceHealth(string Name, string Component, string Status, IReadOnlyList<double> Sparkline);
