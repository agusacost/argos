using Argos.Business.Services.Llm;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Settings;

public sealed class SettingsService(
    ISettingsRepository settings,
    ISettingsValidator validator,
    ILlmService llm,
    IQdrantConfig config,
    ILogger<SettingsService> log) : ISettingsService
{
    public async Task<SettingsDto> GetAsync() => new(await settings.GetAsync(), llm.GetConfiguredKeys());

    public async Task<SettingsDto> UpdateAsync(SettingsPatchRequest patch)
    {
        validator.Validate(patch);
        var recipients = patch.AlertRecipients is null ? null : validator.NormalizeRecipients(patch.AlertRecipients);

        if (!string.IsNullOrEmpty(patch.LlmProvider) && !llm.IsConfigured(patch.LlmProvider))
            throw new InvalidInputException(
                $"Falta {llm.ApiKeyVariableOf(patch.LlmProvider)} en el servidor: no se puede usar '{patch.LlmProvider}'");

        var current = await settings.GetAsync();
        var candidate = current.Copy();
        candidate.ApplyPatch(patch.LlmProvider, patch.LlmModel, patch.SimilarityThreshold, patch.AutoDocEnabled,
            patch.PollingIntervalSeconds, recipients);

        // Si cambia el LLM, validarlo contra el proveedor (key inválida → 422; modelo dudoso → warning).
        string? warning = null;
        if (!candidate.UsesSameLlmAs(current))
        {
            var check = await llm.CheckAsync(llm.Resolve(candidate));
            if (check.Error is not null) throw new InvalidInputException(check.Error);
            warning = check.Warning;
        }

        await settings.SaveAsync(candidate);
        return new SettingsDto(candidate, llm.GetConfiguredKeys(), warning);
    }

    public async Task<RuntimeConfigDto> GetRuntimeConfigAsync()
    {
        var current = await settings.GetAsync();
        var selection = llm.Resolve(current);
        return new RuntimeConfigDto(selection.Provider, selection.Model, current.AutoDocEnabled, current.SimilarityThreshold);
    }

    public async Task<RecipientsResponse> GetAlertRecipientsAsync()
    {
        var recipients = (await settings.GetAsync()).AlertRecipients;
        return new RecipientsResponse(recipients, recipients.Count);
    }

    public async Task<double> GetSimilarityThresholdAsync()
    {
        try
        {
            return (await settings.GetAsync()).SimilarityThreshold;
        }
        catch (Exception ex)
        {
            log.LogWarning("[query] settings no disponibles, uso env: {Error}", ex.Message);
            return config.SimilarityThreshold;
        }
    }
}
