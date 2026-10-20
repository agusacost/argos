using Argos.Business.Seeding;

namespace Argos.Api.Hosting;

/// <summary>Corre la inicialización antes de que Kestrel acepte requests.</summary>
public sealed class StartupHostedService(IStartupInitializer initializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => initializer.InitializeAsync();

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
