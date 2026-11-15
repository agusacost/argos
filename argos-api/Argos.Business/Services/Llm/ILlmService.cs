using Argos.Entities;

namespace Argos.Business.Services.Llm;

/// <summary>Elección del proveedor/modelo LLM, cadena de fallback, salud y generación de texto.</summary>
public interface ILlmService
{
    IReadOnlyList<string> ProviderNames { get; }

    /// <summary>(proveedor, modelo) principal: el modelo de settings solo se usa si corresponde al proveedor.</summary>
    LlmSelection Resolve(AppSettings settings);

    /// <summary>Principal + respaldo de LLM_FALLBACK_CHAIN, sin repetidos (incluye los que no tienen key).</summary>
    IReadOnlyList<LlmSelection> BuildChain(AppSettings settings);

    /// <summary>Proveedor → tiene API key cargada.</summary>
    IReadOnlyDictionary<string, bool> GetConfiguredKeys();

    bool IsConfigured(string provider);

    /// <summary>Si el modelo pertenece al proveedor (false si el proveedor no existe).</summary>
    bool AcceptsModel(string provider, string model);

    /// <summary>Modelo por defecto del proveedor (para los mensajes de error).</summary>
    string DefaultModelOf(string provider);

    /// <summary>Variable de entorno de la key del proveedor (para los mensajes de error).</summary>
    string ApiKeyVariableOf(string provider);

    Task<LlmCheckResult> CheckAsync(LlmSelection selection);

    /// <summary>
    /// Las IP de los prompts se seudonimizan antes de llamar al proveedor y se restauran en el texto devuelto
    /// (LLM_PSEUDONYMIZE_IPS). Recorre la cadena (los eslabones en cooldown van al final) hasta que uno responda.
    /// <see cref="Argos.Common.Exceptions.UpstreamServiceException"/> si fallan todos.
    /// </summary>
    Task<LlmGeneration> GenerateAsync(string systemPrompt, string userPrompt, AppSettings settings);

    /// <summary>
    /// Un único intento con el proveedor/modelo pedido, sin fallback (evaluación por proveedor).
    /// 422 si el proveedor no existe o no tiene key; 502 si falla.
    /// </summary>
    Task<LlmGeneration> GenerateWithAsync(string provider, string? model, string systemPrompt, string userPrompt);

    /// <summary>
    /// Documentación técnica en Markdown, con la misma cadena de fallback. <paramref name="evidence"/>: sección de
    /// evidencia de logs (Loki) ya formateada, o null si no hay.
    /// </summary>
    Task<LlmGeneration> GenerateIncidentDocumentAsync(AppSettings settings, Incident incident, string? evidence = null);

    /// <summary>Estado conocido de cada eslabón de la cadena (sin llamar a los proveedores).</summary>
    IReadOnlyList<LlmProviderHealth> GetHealth(AppSettings settings);

    /// <summary>Chequeo manual: una generación mínima contra cada eslabón con key, y el estado resultante.</summary>
    Task<IReadOnlyList<LlmProviderHealth>> CheckHealthAsync(AppSettings settings);
}
