namespace Argos.Common.Configuration;

/// <summary>Puerto y bindings de Kestrel.</summary>
public interface IHostingConfig
{
    string Port { get; }

    /// <summary>true si ASPNETCORE_URLS o ASPNETCORE_HTTP_PORTS ya definen dónde escuchar.</summary>
    bool HasExplicitUrls { get; }
}
