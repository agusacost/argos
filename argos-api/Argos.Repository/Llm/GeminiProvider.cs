using System.Net;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Entities;

namespace Argos.Repository.Llm;

public sealed class GeminiProvider(IHttpClientFactory httpFactory, ILlmConfig config) : LlmProviderBase(httpFactory)
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    // gemini-flash-latest es "thinking": los thoughts cuentan contra este tope; con 1536 el doc salía truncado.
    private const int MaxOutputTokens = 6144;

    public override string Name => "gemini";
    public override string ModelPrefix => "gemini";
    public override string DefaultModel => config.GeminiModel;
    public override string ApiKeyVariable => "GEMINI_API_KEY";
    public override bool IsConfigured => config.GeminiApiKey.Length > 0;
    protected override string DisplayName => "Gemini";

    private string KeyParam => $"key={Uri.EscapeDataString(config.GeminiApiKey)}";

    protected override async Task<LlmCheckResult> CheckCoreAsync(HttpClient http, string model)
    {
        using var r = await http.GetAsync($"{BaseUrl}/{model}?{KeyParam}");
        var text = await r.Content.ReadAsStringAsync();
        if ((int)r.StatusCode is 400 or 401 or 403 && text.Contains("API key"))
            return LlmCheckResult.Failed($"GEMINI_API_KEY inválida (HTTP {(int)r.StatusCode})");
        if (r.StatusCode == HttpStatusCode.NotFound)
            return LlmCheckResult.Failed($"El modelo '{model}' no existe en Gemini");
        EnsureSuccess(r);
        return LlmCheckResult.Ok;
    }

    public override async Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model)
    {
        var body = new JsonObject
        {
            ["system_instruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = systemPrompt }) },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = userPrompt }),
            }),
            ["generationConfig"] = new JsonObject { ["maxOutputTokens"] = MaxOutputTokens },
        };
        var data = await PostAsync($"{BaseUrl}/{model}:generateContent?{KeyParam}", body);
        return data?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException($"Respuesta inesperada de Gemini: {data?.ToJsonString()}");
    }
}
