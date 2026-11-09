using Argos.Common.Configuration;

namespace Argos.Repository.Llm;

public sealed class GroqProvider(IHttpClientFactory httpFactory, ILlmConfig config) : OpenAiCompatibleProvider(httpFactory)
{
    public override string Name => "groq";
    public override string DefaultModel => config.GroqModel;
    public override string ApiKeyVariable => "GROQ_API_KEY";
    protected override string DisplayName => "Groq";
    protected override string BaseUrl => "https://api.groq.com/openai/v1";
    protected override string ApiKey => config.GroqApiKey;
}
