using Argos.Business.Services.Llm;
using Argos.Business.Services.Users;
using Argos.Common.Configuration;
using Argos.Repository.Embeddings;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Seeding;

/// <summary>
/// Warmup del modelo de embeddings, colecciones, admin inicial, runbooks e incidentes demo, y aviso si
/// falta la API key del LLM configurado.
/// </summary>
public sealed class StartupInitializer(
    ITextEmbedder embedder,
    IIncidentRepository incidents,
    IUserRepository users,
    ISettingsRepository settings,
    IAuditRepository audit,
    INotificationRepository notifications,
    ILoadTestRunRepository loadTests,
    IUserService userService,
    ISeedService seeder,
    ILlmService llm,
    ISeedConfig seedConfig,
    ILlmConfig llmConfig,
    ILogger<StartupInitializer> log) : IStartupInitializer
{
    public async Task InitializeAsync()
    {
        WarmUpEmbeddings();
        try
        {
            await EnsureCollectionsAsync();
            await userService.EnsureAdminAsync();
            if (seedConfig.AutoSeed) await SeedAsync();
            WarnIfLlmKeyMissing();
        }
        catch (Exception ex)
        {
            log.LogWarning("[startup] Init diferido (Qdrant no listo aún): {Error}", ex.Message);
        }
    }

    private void WarmUpEmbeddings()
    {
        try
        {
            embedder.Embed("warmup");
        }
        catch (Exception ex)
        {
            log.LogWarning("[startup] No se pudo precargar embeddings: {Error}", ex.Message);
        }
    }

    private async Task EnsureCollectionsAsync()
    {
        await incidents.EnsureCollectionAsync();
        await users.EnsureCollectionAsync();
        await settings.EnsureCollectionAsync();
        await audit.EnsureCollectionAsync();
        await notifications.EnsureCollectionAsync();
        await loadTests.EnsureCollectionAsync();
    }

    private async Task SeedAsync()
    {
        try { await seeder.SeedKnowledgeAsync(); }
        catch (Exception ex) { log.LogWarning("[seed] seed_knowledge falló: {Error}", ex.Message); }
        try
        {
            if (seedConfig.SeedDemo) await seeder.SeedDemoIncidentsAsync();
        }
        catch (Exception ex) { log.LogWarning("[seed] seed_demo_incidents falló: {Error}", ex.Message); }
    }

    private void WarnIfLlmKeyMissing()
    {
        var provider = llmConfig.LlmProvider;
        if (!llm.IsConfigured(provider))
            log.LogWarning("[startup] ADVERTENCIA: LLM_PROVIDER='{Provider}' pero la API key correspondiente no está " +
                           "configurada. POST /incidents/{{id}}/document fallará con 502.", provider);
    }
}
