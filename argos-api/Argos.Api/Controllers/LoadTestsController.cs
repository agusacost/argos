using Argos.Business.Services.LoadTests;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Vista "Pruebas de Carga": cualquier usuario consulta, solo Admin ejecuta o detiene.</summary>
[Authorize]
[Route("loadtests")]
public sealed class LoadTestsController(ILoadTestService loadTests) : ArgosControllerBase
{
    [HttpGet("scenarios")]
    public IReadOnlyList<LoadTestScenario> Scenarios() => loadTests.GetScenarios();

    [HttpGet("runs")]
    public Task<IReadOnlyList<LoadTestRun>> Runs() => loadTests.ListRunsAsync();

    [HttpGet("runs/{runId}")]
    public Task<LoadTestRun> Run(string runId) => loadTests.GetRunAsync(runId);

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("runs")]
    public async Task<ObjectResult> Start(StartLoadTestRequest request) => Created(await loadTests.StartRunAsync(request));

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("runs/{runId}/stop")]
    public Task<LoadTestRun> Stop(string runId) => loadTests.StopRunAsync(runId);
}
