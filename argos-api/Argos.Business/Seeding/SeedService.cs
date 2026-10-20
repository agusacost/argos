using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Knowledge;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Seeding;

public sealed class SeedService(
    IIncidentRepository incidents,
    IKnowledgeSource knowledge,
    IDemoIncidentCatalog demo,
    IClock clock,
    ILogger<SeedService> log) : ISeedService
{
    public async Task<int> SeedKnowledgeAsync()
    {
        var count = await SaveAllAsync(knowledge.LoadRunbooks(), "runbook");
        log.LogInformation("[seed] knowledge: {Count} runbooks cargados.", count);
        return count;
    }

    public async Task<int> SeedDemoIncidentsAsync()
    {
        await incidents.EnsureCollectionAsync();
        var count = await SaveAllAsync(demo.GetIncidents(), "demo");
        log.LogInformation("[seed] demo: {Count} incidentes demo cargados.", count);
        return count;
    }

    private async Task<int> SaveAllAsync(IEnumerable<Incident> items, string kind)
    {
        var count = 0;
        foreach (var incident in items)
        {
            try
            {
                incident.Fecha ??= clock.UtcNowNaive();
                await incidents.SaveAsync(incident);
                count++;
            }
            catch (Exception ex)
            {
                log.LogWarning("[seed] Error en {Kind} '{Tipo}': {Error}", kind, incident.TipoAlerta, ex.Message);
            }
        }
        return count;
    }
}
