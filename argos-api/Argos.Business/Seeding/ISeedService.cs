namespace Argos.Business.Seeding;

/// <summary>Carga de datos iniciales. Idempotente: los ids de incidentes son determinísticos.</summary>
public interface ISeedService
{
    /// <summary>Runbooks de knowledge/. Devuelve cuántos se cargaron.</summary>
    Task<int> SeedKnowledgeAsync();

    /// <summary>Incidentes demo para poblar el dashboard. Devuelve cuántos se cargaron.</summary>
    Task<int> SeedDemoIncidentsAsync();
}
