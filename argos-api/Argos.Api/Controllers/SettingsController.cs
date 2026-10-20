using Argos.Business.Services.Observability;
using Argos.Business.Services.Settings;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Panel Configuración: settings editables y estado en vivo de las integraciones.</summary>
[Authorize]
public sealed class SettingsController(ISettingsService settings, IPlatformHealthService health) : ArgosControllerBase
{
    [HttpGet("settings")]
    public Task<SettingsDto> Get() => settings.GetAsync();

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("settings")]
    public Task<SettingsDto> Update(SettingsPatchRequest request) => settings.UpdateAsync(request);

    /// <summary>Estado en vivo de Prometheus / Loki / Alertmanager / n8n.</summary>
    [HttpGet("integrations/status")]
    public Task<IReadOnlyList<IntegrationStatus>> Integrations() => health.GetIntegrationsStatusAsync();
}
