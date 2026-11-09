using System.Net;
using Argos.Common.Configuration;
using Argos.Entities;

namespace Argos.Repository.Llm;

public sealed class OpenRouterProvider(IHttpClientFactory httpFactory, ILlmConfig config) : OpenAiCompatibleProvider(httpFactory)
{
    public override string Name => "openrouter";
    public override string DefaultModel => config.OpenRouterModel;
    public override string ApiKeyVariable => "OPENROUTER_API_KEY";
    protected override string DisplayName => "OpenRouter";
    protected override string BaseUrl => "https://openrouter.ai/api/v1";
    protected override string ApiKey => config.OpenRouterApiKey;

    protected override IReadOnlyDictionary<string, string> Headers => new Dictionary<string, string>
    {
        ["Authorization"] = $"Bearer {ApiKey}",
        ["X-Title"] = "Argos AIOps",
    };

    // El listado de modelos de OpenRouter es público: la key se valida aparte con /key.
    protected override async Task<LlmCheckResult> CheckCoreAsync(HttpClient http, string model)
    {
        using (var key = await SendGetAsync(http, "key"))
        {
            if (key.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return InvalidKey(key.StatusCode);
            EnsureSuccess(key);
        }
        return await base.CheckCoreAsync(http, model);
    }
}
