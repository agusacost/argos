namespace Argos.Entities.Dtos;

/// <summary>Config de runtime para n8n (nodo 'Get Config').</summary>
public sealed record RuntimeConfigDto(string LlmProvider, string LlmModel, bool AutoDocEnabled, double SimilarityThreshold);
