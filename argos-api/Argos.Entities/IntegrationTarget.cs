namespace Argos.Entities;

/// <summary>Integración a sondear: <c>up</c> si responde 2xx.</summary>
public sealed record IntegrationTarget(string Key, string Name, string Url);
