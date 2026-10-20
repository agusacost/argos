using System.Text;
using System.Text.Json.Nodes;
using Argos.Entities;
using Argos.Repository.Http;

namespace Argos.Repository.Llm;

/// <summary>Transporte HTTP común y manejo de errores de verificación de los proveedores LLM.</summary>
public abstract class LlmProviderBase(IHttpClientFactory httpFactory) : ILlmProvider
{
    public abstract string Name { get; }
    public abstract string ModelPrefix { get; }
    public abstract string DefaultModel { get; }
    public abstract string ApiKeyVariable { get; }
    public abstract bool IsConfigured { get; }

    public async Task<LlmCheckResult> CheckAsync(string model)
    {
        try
        {
            return await CheckCoreAsync(httpFactory.CreateClient(HttpClientNames.LlmCheck), model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return LlmCheckResult.Warn($"No se pudo verificar el proveedor: {ex.Message}");
        }
    }

    public abstract Task<string> GenerateAsync(string systemPrompt, string userPrompt, string model);

    /// <summary>Verificación propia del proveedor; los errores de red se informan como advertencia.</summary>
    protected abstract Task<LlmCheckResult> CheckCoreAsync(HttpClient http, string model);

    protected async Task<JsonNode?> PostAsync(string url, JsonObject body, IReadOnlyDictionary<string, string>? headers = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        foreach (var (k, v) in headers ?? new Dictionary<string, string>()) req.Headers.Add(k, v);
        using var r = await httpFactory.CreateClient(HttpClientNames.Llm).SendAsync(req);
        var text = await r.Content.ReadAsStringAsync();
        if (!r.IsSuccessStatusCode)
            // Sin la URL: en Gemini lleva la API key en el query string.
            throw new HttpRequestException($"{DisplayName} respondió HTTP {(int)r.StatusCode}: {Truncate(text, 300)}");
        return JsonNode.Parse(text);
    }

    /// <summary>Nombre para mensajes ("Gemini", "Anthropic").</summary>
    protected abstract string DisplayName { get; }

    protected static void EnsureSuccess(HttpResponseMessage r)
    {
        if (!r.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)r.StatusCode} {r.ReasonPhrase}");
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
