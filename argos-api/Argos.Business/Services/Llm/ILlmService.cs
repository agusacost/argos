using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Llm;

/// <summary>Elección del proveedor/modelo LLM y generación de documentación de incidentes.</summary>
public interface ILlmService
{
    IReadOnlyList<string> ProviderNames { get; }

    /// <summary>(proveedor, modelo) efectivos: el modelo de settings solo se usa si corresponde al proveedor.</summary>
    LlmSelection Resolve(AppSettings settings);

    LlmKeysDto GetConfiguredKeys();

    bool IsConfigured(string provider);

    /// <summary>Variable de entorno de la key del proveedor (para los mensajes de error).</summary>
    string ApiKeyVariableOf(string provider);

    Task<LlmCheckResult> CheckAsync(LlmSelection selection);

    /// <summary>Documentación técnica en Markdown. Propaga los errores del proveedor.</summary>
    Task<string> GenerateIncidentDocumentAsync(LlmSelection selection, Incident incident);
}
