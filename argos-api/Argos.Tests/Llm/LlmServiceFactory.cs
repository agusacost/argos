using Argos.Business.Services.Llm;
using Argos.Business.Services.Privacy;
using Argos.Common.Configuration;
using Argos.Repository.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Llm;

/// <summary>LlmService real con los proveedores reales (sin red: las pruebas no llaman a los proveedores).</summary>
public static class LlmServiceFactory
{
    public static Mock<ILlmConfig> Config()
    {
        var config = new Mock<ILlmConfig>();
        config.SetupGet(c => c.LlmProvider).Returns("gemini");
        config.SetupGet(c => c.GeminiModel).Returns("gemini-flash-lite-latest");
        config.SetupGet(c => c.ClaudeModel).Returns("claude-sonnet-4-6");
        config.SetupGet(c => c.GroqModel).Returns("llama-3.3-70b-versatile");
        config.SetupGet(c => c.OpenRouterModel).Returns("nvidia/nemotron-3-ultra-550b-a55b:free");
        config.SetupGet(c => c.GeminiApiKey).Returns("");
        config.SetupGet(c => c.AnthropicApiKey).Returns("");
        config.SetupGet(c => c.GroqApiKey).Returns("");
        config.SetupGet(c => c.OpenRouterApiKey).Returns("");
        config.SetupGet(c => c.LlmFallbackChain).Returns("");
        return config;
    }

    public static LlmService WithRealProviders()
    {
        var http = Mock.Of<IHttpClientFactory>();
        var config = Config().Object;
        ILlmProvider[] providers =
        [
            new GeminiProvider(http, config), new AnthropicProvider(http, config),
            new GroqProvider(http, config), new OpenRouterProvider(http, config),
        ];
        return Create(providers, pseudonymizeIps: true, config);
    }

    public static LlmService Create(IEnumerable<ILlmProvider> providers, bool pseudonymizeIps, ILlmConfig? config = null)
    {
        var privacy = new Mock<IPrivacyConfig>();
        privacy.SetupGet(p => p.PseudonymizeIps).Returns(pseudonymizeIps);
        return new LlmService(providers, config ?? Config().Object, Mock.Of<ILlmHealthTracker>(),
            new IpPseudonymizer(privacy.Object), NullLogger<LlmService>.Instance);
    }
}
