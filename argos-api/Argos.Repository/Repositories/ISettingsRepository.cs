using Argos.Entities;

namespace Argos.Repository.Repositories;

/// <summary>Documento único de configuración.</summary>
public interface ISettingsRepository
{
    Task EnsureCollectionAsync();

    /// <summary>Lo guardado sobre los defaults (sin claves obsoletas).</summary>
    Task<AppSettings> GetAsync();

    Task SaveAsync(AppSettings settings);
}
