using Argos.Entities.Dtos;

namespace Argos.Business.Services.Settings;

/// <summary>Reglas de la configuración editable. Lanzan 422 con un mensaje legible.</summary>
public interface ISettingsValidator
{
    void Validate(SettingsPatchRequest patch);

    /// <summary>strip + lowercase + dedup (conserva el orden); valida formato y tope de destinatarios.</summary>
    IReadOnlyList<string> NormalizeRecipients(IEnumerable<string?> emails);
}
