using Argos.Api.Filters;
using Argos.Business.Services.Settings;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Configuración que n8n lee en cada alerta (token interno opcional).</summary>
[RequireInternalToken]
public sealed class InternalController(ISettingsService settings) : ArgosControllerBase
{
    /// <summary>Pool de destinatarios del email (nodo 'Get Recipients').</summary>
    [HttpGet("notifications/recipients")]
    public Task<RecipientsResponse> Recipients() => settings.GetAlertRecipientsAsync();

    /// <summary>LLM efectivo, auto-doc y umbral (nodo 'Get Config').</summary>
    [HttpGet("internal/config")]
    public Task<RuntimeConfigDto> Config() => settings.GetRuntimeConfigAsync();
}
