using Argos.Entities.Dtos;

namespace Argos.Business.Services.Settings;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync();

    /// <summary>Valida, verifica el LLM contra el proveedor si cambió y guarda. 422 si algo no es válido.</summary>
    Task<SettingsDto> UpdateAsync(SettingsPatchRequest patch);

    /// <summary>LLM efectivo, auto-doc y umbral (n8n, nodo 'Get Config').</summary>
    Task<RuntimeConfigDto> GetRuntimeConfigAsync();

    /// <summary>Pool de destinatarios del email de alertas (n8n, nodo 'Get Recipients').</summary>
    Task<RecipientsResponse> GetAlertRecipientsAsync();

    /// <summary>Umbral vigente: el de settings o, si no se puede leer, SIMILARITY_THRESHOLD.</summary>
    Task<double> GetSimilarityThresholdAsync();
}
