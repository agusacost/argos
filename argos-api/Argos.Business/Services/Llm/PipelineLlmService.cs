using Argos.Business.Services.Notifications;
using Argos.Common.Exceptions;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Llm;

public sealed class PipelineLlmService(
    ILlmService llm,
    ISettingsRepository settings,
    INotificationService notifications,
    ILogger<PipelineLlmService> log) : IPipelineLlmService
{
    public async Task<LlmGeneration> GenerateAsync(LlmGenerateRequest request)
    {
        // Evaluación por proveedor: sin fallback y sin aviso en la campana (no es una alerta real).
        if (request.ForcesProvider)
            return await llm.GenerateWithAsync(request.Provider!, request.Model, request.System!, request.User!);
        try
        {
            return await llm.GenerateAsync(request.System!, request.User!, await settings.GetAsync());
        }
        catch (UpstreamServiceException ex)
        {
            try
            {
                await notifications.NotifyPipelineFailureAsync(request.Purpose ?? LlmGenerateRequest.AnalysisPurpose,
                    request.Alertname, request.Severity, ex.Detail);
            }
            catch (Exception notifyError)
            {
                log.LogWarning("[llm] no se pudo guardar el aviso de falla del pipeline: {Error}", notifyError.Message);
            }
            throw;
        }
    }
}
