using System.Net;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Entities;

namespace Argos.Repository.Llm;

public sealed class AnthropicProvider(IHttpClientFactory httpFactory, ILlmConfig config) : LlmProviderBase(httpFactory)
{
    private const string BaseUrl = "https://api.anthropic.com/v1";
    private const string ApiVersion = "2023-06-01";
    private const int MaxTokens = 1536;

    public override string Name => "anthropic";
    public override string DefaultModel => config.ClaudeModel;
    public override string ApiKeyVariable => "ANTHROPIC_API_KEY";
    public override bool IsConfigured => config.AnthropicApiKey.Length > 0;
    protected override string DisplayName => "Anthropic";

    private Dictionary<string, string> Headers => new()
    {
        ["x-api-key"] = config.AnthropicApiKey,
        ["anthropic-version"] = ApiVersion,
    };

    public override bool AcceptsModel(string model) => model.StartsWith("claude", StringComparison.OrdinalIgnoreCase);

    protected override async Task<LlmCheckResult> CheckCoreAsync(HttpClient http, string model)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models?limit=1000");
        foreach (var (k, v) in Headers) req.Headers.Add(k, v);
        using var r = await http.SendAsync(req);
        if (r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return LlmCheckResult.Failed($"ANTHROPIC_API_KEY inválida o sin permisos (HTTP {(int)r.StatusCode})");
        EnsureSuccess(r);
        var data = JsonNode.Parse(await r.Content.ReadAsStringAsync())?["data"] as JsonArray ?? [];
        var ids = data.Select(m => m?["id"]?.GetValue<string>()).Where(i => i is not null).ToHashSet();
        return ids.Count > 0 && !ids.Contains(model)
            ? LlmCheckResult.Warn($"El modelo '{model}' no figura en la lista de Anthropic (puede ser un alias)")
            : LlmCheckResult.Ok;
    }

    public override Task PingAsync(string model) =>
        PostAsync($"{BaseUrl}/messages", Body("Respondé solo: ok", "ping", model, 1), Headers);

    public override async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model)
    {
        var data = await PostAsync($"{BaseUrl}/messages", Body(systemPrompt, userPrompt, model, MaxTokens), Headers);
        return data?["content"]?[0]?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException($"Respuesta inesperada de Anthropic: {data?.ToJsonString()}");
    }

    private static JsonObject Body(string systemPrompt, string userPrompt, string model, int maxTokens) => new()
    {
        ["model"] = model,
        ["max_tokens"] = maxTokens,
        ["system"] = systemPrompt,
        ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = userPrompt }),
    };
}
