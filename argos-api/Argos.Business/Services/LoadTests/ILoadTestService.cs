using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.LoadTests;

/// <summary>Pruebas de carga k6 on-demand desde el dashboard (una corrida a la vez).</summary>
public interface ILoadTestService
{
    /// <summary>Escenarios con el código de su script.</summary>
    IReadOnlyList<LoadTestScenario> GetScenarios();

    Task<IReadOnlyList<LoadTestRun>> ListRunsAsync();

    /// <summary>404 si no existe.</summary>
    Task<LoadTestRun> GetRunAsync(string runId);

    /// <summary>
    /// Lanza k6 y deja su cierre en segundo plano. 409 si ya hay una corrida en ejecución,
    /// 400 si el escenario no existe o Docker / el script no están disponibles.
    /// </summary>
    Task<LoadTestRun> StartRunAsync(StartLoadTestRequest request);

    /// <summary>Detiene el contenedor y marca la corrida como cancelada. 404 si no existe.</summary>
    Task<LoadTestRun> StopRunAsync(string runId);
}
