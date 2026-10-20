using Argos.Entities;

namespace Argos.Repository.Llm;

/// <summary>Proveedor de LLM (Gemini o Anthropic). Las API keys viven solo en el entorno.</summary>
public interface ILlmProvider
{
    /// <summary>Valor de <c>llm_provider</c>: "gemini" o "anthropic".</summary>
    string Name { get; }

    /// <summary>Prefijo que deben tener los modelos de este proveedor ("gemini" / "claude").</summary>
    string ModelPrefix { get; }

    string DefaultModel { get; }

    /// <summary>Variable de entorno de la API key (para los mensajes de error).</summary>
    string ApiKeyVariable { get; }

    bool IsConfigured { get; }

    /// <summary>Valida key + modelo con los endpoints de listado (sin costo de tokens).</summary>
    Task<LlmCheckResult> CheckAsync(string model);

    /// <summary>Texto generado. Lanza <see cref="HttpRequestException"/> si el proveedor falla.</summary>
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model);
}
