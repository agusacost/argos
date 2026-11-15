using Argos.Api.Filters;
using Argos.Business.Services.Evidence;
using Argos.Business.Services.Incidents;
using Argos.Business.Services.Llm;
using Argos.Business.Services.Settings;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Endpoints que n8n usa en cada alerta (token interno opcional).</summary>
[RequireInternalToken]
public sealed class InternalController(
    ISettingsService settings,
    IPipelineLlmService llm,
    IAlertLifecycleService alerts,
    IAlertEvidenceService evidence) : ArgosControllerBase
{
    /// <summary>Pool de destinatarios del email (nodo 'Get Recipients').</summary>
    [HttpGet("notifications/recipients")]
    public Task<RecipientsResponse> Recipients() => settings.GetAlertRecipientsAsync();

    /// <summary>LLM efectivo, auto-doc y umbral (nodo 'Get Config').</summary>
    [HttpGet("internal/config")]
    public Task<RuntimeConfigDto> Config() => settings.GetRuntimeConfigAsync();

    /// <summary>
    /// Evidencia acotada de logs (Loki) de la ventana de la alerta (nodo 'Get Evidence', antes de 'Build Prompt').
    /// Nunca falla por Loki: sin datos devuelve <c>available=false</c> y <c>text</c> vacío.
    /// </summary>
    [HttpPost("internal/evidence")]
    public Task<AlertEvidence> Evidence(AlertEvidenceRequest request) =>
        evidence.CollectAsync(request.Alertname!.Trim(), request.Labels, IsoDateTime.Parse(request.StartsAt),
            IsoDateTime.Parse(request.EndsAt));

    /// <summary>Análisis y documentación (nodos 'LLM Call' / 'Doc LLM Call'); 502 si falla toda la cadena.</summary>
    [HttpPost("internal/llm/generate")]
    public Task<LlmGeneration> Generate(LlmGenerateRequest request) => llm.GenerateAsync(request);

    /// <summary>Alerta ya documentada: incidente abierto con la solución conocida (nodo 'Register Occurrence').</summary>
    [HttpPost("internal/incidents/occurrence")]
    public Task<AlertOccurrenceResponse> Occurrence(AlertOccurrenceRequest request) => alerts.RegisterOccurrenceAsync(request);

    /// <summary>Alertmanager avisó la resolución (nodo 'Resolve Incident').</summary>
    [HttpPost("internal/incidents/resolve")]
    public Task<AlertResolutionResponse> Resolve(AlertResolutionRequest request) => alerts.ResolveAsync(request);
}
