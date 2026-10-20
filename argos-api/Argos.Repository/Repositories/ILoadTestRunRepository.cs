using Argos.Entities;

namespace Argos.Repository.Repositories;

public interface ILoadTestRunRepository : IRepository<LoadTestRun>
{
    /// <summary>La corrida en estado <c>running</c>, si hay una.</summary>
    Task<LoadTestRun?> FindRunningAsync();
}
