using Argos.Common.Configuration;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Llm;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Llm;

public sealed class LlmService : ILlmService
{
    private const string FallbackProvider = "gemini";

    private readonly IReadOnlyDictionary<string, ILlmProvider> _providers;
    private readonly ILlmConfig _config;
    private readonly ILogger<LlmService> _log;

    public LlmService(IEnumerable<ILlmProvider> providers, ILlmConfig config, ILogger<LlmService> log)
    {
        _providers = providers.ToDictionary(p => p.Name);
        _config = config;
        _log = log;
    }

    public IReadOnlyList<string> ProviderNames => [.. _providers.Keys];

    public LlmSelection Resolve(AppSettings settings)
    {
        var name = (string.IsNullOrEmpty(settings.LlmProvider) ? _config.LlmProvider : settings.LlmProvider).ToLowerInvariant();
        var provider = _providers.GetValueOrDefault(name) ?? _providers[FallbackProvider];
        var model = (settings.LlmModel ?? "").Trim();
        if (!model.ToLowerInvariant().StartsWith(provider.ModelPrefix, StringComparison.Ordinal))
            model = provider.DefaultModel;
        return new LlmSelection(provider.Name, model);
    }

    public LlmKeysDto GetConfiguredKeys() => new(IsConfigured("gemini"), IsConfigured("anthropic"));

    public bool IsConfigured(string provider) => _providers.TryGetValue(provider, out var p) && p.IsConfigured;

    public string ApiKeyVariableOf(string provider) => ProviderFor(provider).ApiKeyVariable;

    public Task<LlmCheckResult> CheckAsync(LlmSelection selection) =>
        ProviderFor(selection.Provider).CheckAsync(selection.Model);

    public Task<string> GenerateIncidentDocumentAsync(LlmSelection selection, Incident incident)
    {
        var provider = ProviderFor(selection.Provider);
        var model = string.IsNullOrEmpty(selection.Model) ? provider.DefaultModel : selection.Model;
        var prompt = new IncidentDocumentPrompt(incident);
        _log.LogInformation("[llm] generate_document provider={Provider} model={Model}", provider.Name, model);
        return provider.GenerateAsync(prompt.System, prompt.UserPrompt, model);
    }

    private ILlmProvider ProviderFor(string name) => _providers.GetValueOrDefault(name) ?? _providers[FallbackProvider];
}
