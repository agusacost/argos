using System.Diagnostics;
using System.Text.Json;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Entities;
using Argos.Repository.Llm;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Llm;

public sealed class LlmService : ILlmService
{
    private const string FallbackProvider = "gemini";
    private const string PrimaryRole = "primary";
    private const string FallbackRole = "fallback";

    private readonly IReadOnlyDictionary<string, ILlmProvider> _providers;
    private readonly ILlmConfig _config;
    private readonly ILlmHealthTracker _health;
    private readonly ILogger<LlmService> _log;

    public LlmService(IEnumerable<ILlmProvider> providers, ILlmConfig config, ILlmHealthTracker health, ILogger<LlmService> log)
    {
        _providers = providers.ToDictionary(p => p.Name);
        _config = config;
        _health = health;
        _log = log;
    }

    public IReadOnlyList<string> ProviderNames => [.. _providers.Keys];

    public LlmSelection Resolve(AppSettings settings)
    {
        var name = (string.IsNullOrEmpty(settings.LlmProvider) ? _config.LlmProvider : settings.LlmProvider).ToLowerInvariant();
        var provider = _providers.GetValueOrDefault(name) ?? _providers[FallbackProvider];
        var model = (settings.LlmModel ?? "").Trim();
        return new LlmSelection(provider.Name, provider.AcceptsModel(model) ? model : provider.DefaultModel);
    }

    public IReadOnlyList<LlmSelection> BuildChain(AppSettings settings)
    {
        var chain = new List<LlmSelection> { Resolve(settings) };
        foreach (var entry in _config.LlmFallbackChain.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Split en 2: los modelos de OpenRouter llevan ':' (":free").
            var parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
            if (!_providers.TryGetValue(parts[0].ToLowerInvariant(), out var provider))
            {
                _log.LogWarning("[llm] LLM_FALLBACK_CHAIN: proveedor desconocido '{Provider}'", parts[0]);
                continue;
            }
            var model = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : provider.DefaultModel;
            var selection = new LlmSelection(provider.Name, model);
            if (!chain.Contains(selection)) chain.Add(selection);
        }
        return chain;
    }

    public IReadOnlyDictionary<string, bool> GetConfiguredKeys() =>
        _providers.ToDictionary(p => p.Key, p => p.Value.IsConfigured);

    public bool IsConfigured(string provider) => _providers.TryGetValue(provider, out var p) && p.IsConfigured;

    public string ApiKeyVariableOf(string provider) => ProviderFor(provider).ApiKeyVariable;

    public Task<LlmCheckResult> CheckAsync(LlmSelection selection) =>
        ProviderFor(selection.Provider).CheckAsync(selection.Model);

    public async Task<LlmGeneration> GenerateAsync(string systemPrompt, string userPrompt, AppSettings settings)
    {
        var primary = Resolve(settings);
        var chain = BuildChain(settings).Where(s => IsConfigured(s.Provider)).ToList();
        if (chain.Count == 0)
            throw new UpstreamServiceException("No hay ningún proveedor LLM con API key configurada");

        var available = chain.Where(_health.IsAvailable).ToList();
        var attempts = new List<LlmAttempt>();
        foreach (var selection in available.Concat(chain.Except(available)))
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var text = await ProviderFor(selection.Provider).GenerateAsync(systemPrompt, userPrompt, selection.Model);
                await _health.RecordSuccessAsync(selection, watch.ElapsedMilliseconds);
                attempts.Add(new LlmAttempt(selection.Provider, selection.Model, true, null, watch.ElapsedMilliseconds));
                _log.LogInformation("[llm] generate provider={Provider} model={Model} intentos={Attempts}",
                    selection.Provider, selection.Model, attempts.Count);
                return new LlmGeneration(text, selection.Provider, selection.Model, selection != primary, attempts);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
            {
                var error = LlmErrorDescriber.Describe(ex);
                attempts.Add(new LlmAttempt(selection.Provider, selection.Model, false, error, watch.ElapsedMilliseconds));
                await _health.RecordFailureAsync(selection, error, watch.ElapsedMilliseconds);
                _log.LogWarning("[llm] {Provider}/{Model} falló ({Error}); pruebo el siguiente", selection.Provider, selection.Model, error);
            }
        }
        throw new UpstreamServiceException(
            "Ningún proveedor LLM respondió: " + string.Join("; ", attempts.Select(a => $"{a.Provider}/{a.Model} → {a.Error}")));
    }

    public async Task<LlmGeneration> GenerateWithAsync(string provider, string? model, string systemPrompt, string userPrompt)
    {
        if (!_providers.TryGetValue(provider.Trim().ToLowerInvariant(), out var selected))
            throw new InvalidInputException($"Campo 'provider': debe ser uno de {string.Join(", ", ProviderNames)}");
        if (!selected.IsConfigured)
            throw new InvalidInputException($"El proveedor '{selected.Name}' no tiene API key ({selected.ApiKeyVariable})");

        var selection = new LlmSelection(selected.Name, string.IsNullOrWhiteSpace(model) ? selected.DefaultModel : model.Trim());
        var watch = Stopwatch.StartNew();
        try
        {
            var text = await selected.GenerateAsync(systemPrompt, userPrompt, selection.Model);
            await _health.RecordSuccessAsync(selection, watch.ElapsedMilliseconds);
            return new LlmGeneration(text, selection.Provider, selection.Model, false,
                [new LlmAttempt(selection.Provider, selection.Model, true, null, watch.ElapsedMilliseconds)]);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            var error = LlmErrorDescriber.Describe(ex);
            await _health.RecordFailureAsync(selection, error, watch.ElapsedMilliseconds);
            throw new UpstreamServiceException($"{selection.Provider}/{selection.Model} → {error}");
        }
    }

    public Task<LlmGeneration> GenerateIncidentDocumentAsync(AppSettings settings, Incident incident)
    {
        var prompt = new IncidentDocumentPrompt(incident);
        return GenerateAsync(prompt.System, prompt.UserPrompt, settings);
    }

    public IReadOnlyList<LlmProviderHealth> GetHealth(AppSettings settings)
    {
        var primary = Resolve(settings);
        return BuildChain(settings)
            .Select(s => _health.Describe(s, s == primary ? PrimaryRole : FallbackRole, IsConfigured(s.Provider)))
            .ToList();
    }

    public async Task<IReadOnlyList<LlmProviderHealth>> CheckHealthAsync(AppSettings settings)
    {
        await Task.WhenAll(BuildChain(settings).Where(s => IsConfigured(s.Provider)).Select(PingAsync));
        return GetHealth(settings);
    }

    private async Task PingAsync(LlmSelection selection)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            await ProviderFor(selection.Provider).PingAsync(selection.Model);
            await _health.RecordSuccessAsync(selection, watch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            await _health.RecordFailureAsync(selection, LlmErrorDescriber.Describe(ex), watch.ElapsedMilliseconds);
        }
    }

    private ILlmProvider ProviderFor(string name) => _providers.GetValueOrDefault(name) ?? _providers[FallbackProvider];
}
