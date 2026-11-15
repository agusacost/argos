using Argos.Business.Services.Settings;
using Argos.Common.Exceptions;
using Argos.Tests.Llm;

namespace Argos.Tests.Settings;

/// <summary>F-10: un modelo de otro proveedor se rechaza con 422 y un detail claro.</summary>
public sealed class SettingsValidatorTests
{
    private readonly SettingsValidator _validator = new(LlmServiceFactory.WithRealProviders());

    [Theory]
    [InlineData("gemini", "gemini-flash-lite-latest")]
    [InlineData("gemini", "Gemini-Flash-Latest")]
    [InlineData("anthropic", "claude-sonnet-4-6")]
    [InlineData("groq", "llama-3.3-70b-versatile")]
    [InlineData("openrouter", "nvidia/nemotron-3-ultra-550b-a55b:free")]
    public void MatchingModel_IsAccepted(string provider, string model) =>
        _validator.ValidateModelForProvider(provider, model);

    [Theory]
    [InlineData("gemini", "modelo-sin-prefijo-e2e")]
    [InlineData("gemini", "claude-sonnet-4-6")]
    [InlineData("anthropic", "gemini-flash-latest")]
    [InlineData("groq", "gemini-flash-latest")]
    [InlineData("openrouter", "claude-sonnet-4-6")]
    public void ForeignModel_IsRejectedWithDetail(string provider, string model)
    {
        var ex = Assert.Throws<InvalidInputException>(() => _validator.ValidateModelForProvider(provider, model));

        Assert.Contains($"'{model}'", ex.Detail);
        Assert.Contains($"'{provider}'", ex.Detail);
    }
}
