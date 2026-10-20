using Argos.Entities;
using Argos.Repository.Qdrant;

namespace Argos.Repository.Repositories;

/// <summary>
/// Configuración del dashboard: un único punto con id fijo en <c>argos_settings</c> (vector dummy).
/// No es una <see cref="Entity"/>: no tiene id propio en la API.
/// </summary>
public sealed class SettingsRepository(IQdrantClient client, IPayloadSerializer serializer) : ISettingsRepository
{
    private const string SettingsId = "00000000-0000-0000-0000-000000000001";
    private static readonly float[] DummyVector = [1.0f];

    public async Task EnsureCollectionAsync()
    {
        var existing = await client.GetCollectionNamesAsync();
        if (!existing.Contains(CollectionNames.Settings))
            await client.CreateCollectionAsync(CollectionNames.Settings, 1, "Dot");
    }

    public async Task<AppSettings> GetAsync()
    {
        var point = await client.RetrieveAsync(CollectionNames.Settings, SettingsId);
        var settings = point is null ? new AppSettings() : serializer.Deserialize<AppSettings>(point.Payload);
        settings.RemoveObsoleteFields();
        return settings;
    }

    /// <summary>Reemplaza el documento completo (así se descartan las claves obsoletas guardadas).</summary>
    public Task SaveAsync(AppSettings settings) =>
        client.UpsertAsync(CollectionNames.Settings, SettingsId, DummyVector, serializer.Serialize(settings));
}
