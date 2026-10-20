using Argos.Entities;

namespace Argos.Business.Services.LoadTests;

public interface ILoadTestScenarioCatalog
{
    IReadOnlyList<LoadTestScenario> All { get; }

    LoadTestScenario? Find(string scenarioId);
}
