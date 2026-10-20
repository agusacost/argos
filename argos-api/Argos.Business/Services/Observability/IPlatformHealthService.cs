using Argos.Entities;

namespace Argos.Business.Services.Observability;

/// <summary>Estado en vivo de los servicios de la plataforma y de las integraciones.</summary>
public interface IPlatformHealthService
{
    /// <summary>Nginx, API .NET, RAG API y Qdrant: healthy si responden &lt;500.</summary>
    Task<IReadOnlyList<ServiceHealth>> GetServicesHealthAsync();

    /// <summary>Prometheus, Loki, Alertmanager y n8n: up si responden 2xx.</summary>
    Task<IReadOnlyList<IntegrationStatus>> GetIntegrationsStatusAsync();
}
