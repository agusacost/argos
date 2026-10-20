using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.LoadTests;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.LoadTests;

/// <summary>
/// start → crea el registro, copia script + datasets al volumen compartido y lanza el contenedor k6;
/// finalize (segundo plano) → espera el contenedor, lee el summary y cierra el registro.
/// </summary>
public sealed class LoadTestService(
    ILoadTestScenarioCatalog scenarios,
    ILoadTestRunRepository runs,
    IK6ContainerRunner runner,
    IK6Workspace workspace,
    ILoadTestConfig config,
    IClock clock,
    ILogger<LoadTestService> log) : ILoadTestService
{
    private const int MaxListedRuns = 20;
    private const int LogTailLines = 40;
    private const string MissingScript = "// (código no disponible: revisar el bind de k6/tests en rag-api)";

    public IReadOnlyList<LoadTestScenario> GetScenarios() =>
        scenarios.All.Select(s => s.WithCode(workspace.ReadScript(s.Archivo) ?? MissingScript)).ToList();

    public async Task<IReadOnlyList<LoadTestRun>> ListRunsAsync() =>
        (await runs.ListAsync()).OrderByDescending(r => r.StartedAt, StringComparer.Ordinal).Take(MaxListedRuns).ToList();

    public async Task<LoadTestRun> GetRunAsync(string runId) =>
        await runs.GetByIdAsync(runId) ?? throw new NotFoundException("Corrida no encontrada");

    public async Task<LoadTestRun> StartRunAsync(StartLoadTestRequest request)
    {
        var scenario = scenarios.Find(request.Scenario!)
                       ?? throw new BadRequestException($"Escenario desconocido: {request.Scenario}");
        if (await runs.FindRunningAsync() is not null)
            throw new ConflictException("Ya hay una prueba de carga en ejecución");
        var baseUrl = string.IsNullOrEmpty(request.BaseUrl) ? config.K6DefaultBaseUrl : request.BaseUrl;

        try
        {
            await runner.EnsureAvailableAsync();
        }
        catch (ExternalServiceException ex)
        {
            throw new BadRequestException(ex.Message, ex);
        }

        // Registro previo para obtener el id (el summary se guarda con el nombre del run).
        var run = await runs.SaveAsync(new LoadTestRun
        {
            Scenario = scenario.Id,
            ScenarioName = scenario.Name,
            BaseUrl = baseUrl,
            StartedAt = clock.UtcNowAware(),
        });

        try
        {
            workspace.PrepareScript(scenario.Archivo);
        }
        catch (ExternalServiceException ex)
        {
            throw await FailAsync(run, $"No se pudo preparar el script: {ex.Message}", ex);
        }

        try
        {
            run.AttachContainer(await runner.StartAsync(run.Id!, scenario.Archivo, baseUrl));
        }
        catch (ExternalServiceException ex)
        {
            throw await FailAsync(run, $"No se pudo lanzar k6: {ex.Message}", ex);
        }
        await runs.UpdateAsync(run);

        _ = Task.Run(() => FinalizeRunAsync(run.Id!));
        return run;
    }

    public async Task<LoadTestRun> StopRunAsync(string runId)
    {
        var run = await GetRunAsync(runId);
        if (run.HasContainer) await runner.StopAsync(run.ContainerId!);
        run.Cancel(clock.UtcNowAware());
        await runs.UpdateAsync(run);
        return run;
    }

    /// <summary>Segundo plano: espera el contenedor, parsea el summary y cierra el registro.</summary>
    private async Task FinalizeRunAsync(string runId)
    {
        try
        {
            var run = await runs.GetByIdAsync(runId);
            if (run is null || !run.HasContainer) return;
            try
            {
                var result = await runner.WaitAsync(run.ContainerId!);
                run.Complete(result.ExitCode, workspace.ReadSummary(runId), LogTail(result.Logs), clock.UtcNowAware());
                await runs.UpdateAsync(run);
                await runner.RemoveAsync(run.ContainerId!);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[loadtest] Error al finalizar {RunId}", runId);
                run.Fail($"Error al finalizar: {ex.Message}", clock.UtcNowAware());
                await runs.UpdateAsync(run);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "[loadtest] finalize {RunId}", runId);
        }
    }

    private async Task<BadRequestException> FailAsync(LoadTestRun run, string reason, Exception cause)
    {
        run.Fail(reason, clock.UtcNowAware());
        await runs.UpdateAsync(run);
        return new BadRequestException(reason, cause);
    }

    private static string LogTail(string logs)
    {
        var lines = logs.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines.TakeLast(LogTailLines));
    }
}
