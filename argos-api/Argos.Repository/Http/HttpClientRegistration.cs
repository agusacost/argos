using Microsoft.Extensions.DependencyInjection;

namespace Argos.Repository.Http;

/// <summary>
/// Registra los clientes HTTP con nombre. Va en <see cref="IServiceCollection"/> porque
/// <c>IHttpClientFactory</c> se configura ahí; Autofac lo recibe al poblar el contenedor.
/// </summary>
public static class HttpClientRegistration
{
    public static IServiceCollection AddArgosHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientNames.Qdrant, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient(HttpClientNames.Observability, c => c.Timeout = TimeSpan.FromSeconds(8));
        services.AddHttpClient(HttpClientNames.Probe, c => c.Timeout = TimeSpan.FromSeconds(4));
        services.AddHttpClient(HttpClientNames.Llm, c => c.Timeout = TimeSpan.FromSeconds(90));
        services.AddHttpClient(HttpClientNames.LlmCheck, c => c.Timeout = TimeSpan.FromSeconds(8));
        return services;
    }
}
