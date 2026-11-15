namespace Argos.Entities.Dtos;

/// <summary>Config de runtime para n8n (nodo 'Get Config'). <c>EnvironmentContext</c> es el contexto del entorno vigente
/// (el editado en Configuración o, si nunca se editó, el del despliegue).</summary>
public sealed record RuntimeConfigDto(string LlmProvider, string LlmModel, bool AutoDocEnabled, double SimilarityThreshold,
    string EnvironmentContext);
