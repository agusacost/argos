using System.Net;
using System.Text.Json.Nodes;
using Argos.Entities;

namespace Argos.Repository.Llm;

/// <summary>Proveedores con la API de chat completions de OpenAI (Groq, OpenRouter).</summary>
public abstract class OpenAiCompatibleProvider(IHttpClientFactory httpFactory) : LlmProviderBase(httpFactory)
{
    private const int MaxTokens = 4096;

    protected abstract string BaseUrl { get; }
    protected abstract string ApiKey { get; }

    public override bool IsConfigured => ApiKey.Length > 0;

    /// <summary>Cualquier id de modelo salvo los de Gemini/Claude (restos de cambiar de proveedor).</summary>
    public override bool AcceptsModel(string model) =>
        model.Length > 0 &&
        !model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase) &&
        !model.StartsWith("claude", StringComparison.OrdinalIgnoreCase);

    protected virtual IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>
    {
        ["Authorization"] = $"Bearer {ApiKey}",
    };

    protected override async Task<LlmCheckResult> CheckCoreAsync(HttpClient http, string model)
    {
        using var r = await SendGetAsync(http, "models");
        if (r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return InvalidKey(r.StatusCode);
        EnsureSuccess(r);
        var data = JsonNode.Parse(await r.Content.ReadAsStringAsync())?["data"] as JsonArray ?? [];
        var ids = data.Select(m => m?["id"]?.GetValue<string>()).Where(i => i is not null).ToHashSet();
        return ids.Count > 0 && !ids.Contains(model)
            ? LlmCheckResult.Failed($"El modelo '{model}' no existe en {DisplayName}")
            : LlmCheckResult.Ok;
    }

    public override Task PingAsync(string model) =>
        PostAsync($"{BaseUrl}/chat/completions", Body("Respondé solo: ok", "ping", model, 1), Headers);

    public override async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model)
    {
        var data = await PostAsync($"{BaseUrl}/chat/completions", Body(systemPrompt, userPrompt, model, MaxTokens), Headers);
        var text = data?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException($"Respuesta vacía de {DisplayName}: {data?.ToJsonString()}")
            : text;
    }

    protected async Task<HttpResponseMessage> SendGetAsync(HttpClient http, string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{path}");
        foreach (var (k, v) in Headers) req.Headers.Add(k, v);
        return await http.SendAsync(req);
    }

    protected LlmCheckResult InvalidKey(HttpStatusCode status) =>
        LlmCheckResult.Failed($"{ApiKeyVariable} inválida o sin permisos (HTTP {(int)status})");

    private static JsonObject Body(string systemPrompt, string userPrompt, string model, int maxTokens) => new()
    {
        ["model"] = model,
        ["max_tokens"] = maxTokens,
        ["messages"] = new JsonArray(
            new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
            new JsonObject { ["role"] = "user", ["content"] = userPrompt }),
    };
}
