using Argos.Entities;

namespace Argos.Repository.Llm;

/// <summary>Proveedor de LLM (Gemini, Anthropic, Groq, OpenRouter). Las API keys viven solo en el entorno.</summary>
public interface ILlmProvider
{
    /// <summary>Valor de <c>llm_provider</c>: "gemini", "anthropic", "groq" u "openrouter".</summary>
    string Name { get; }

    string DefaultModel { get; }

    /// <summary>Variable de entorno de la API key (para los mensajes de error).</summary>
    string ApiKeyVariable { get; }

    bool IsConfigured { get; }

    /// <summary>Si el modelo pertenece a este proveedor (un modelo ajeno se reemplaza por el default).</summary>
    bool AcceptsModel(string model);

    /// <summary>Valida key + modelo con los endpoints de listado (sin costo de tokens).</summary>
    Task<LlmCheckResult> CheckAsync(string model);

    /// <summary>Generación mínima real: detecta lo que el listado no ve (modelo saturado, cuota agotada).</summary>
    Task PingAsync(string model);

    /// <summary>Texto generado. Lanza <see cref="HttpRequestException"/> si el proveedor falla.</summary>
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model);
}
