using Argos.Business.Services.Llm;
using Argos.Entities;
using Argos.Repository.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Salud de la cadena LLM (principal + respaldo) para el header y Configuración.</summary>
[Authorize]
public sealed class LlmController(ILlmService llm, ISettingsRepository settings) : ArgosControllerBase
{
    /// <summary>Estado según las últimas llamadas reales (no consume tokens).</summary>
    [HttpGet("llm/health")]
    public async Task<IReadOnlyList<LlmProviderHealth>> Health() => llm.GetHealth(await settings.GetAsync());

    /// <summary>"Probar ahora": una generación mínima contra cada proveedor con key.</summary>
    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("llm/health/check")]
    public async Task<IReadOnlyList<LlmProviderHealth>> Check() => await llm.CheckHealthAsync(await settings.GetAsync());
}
