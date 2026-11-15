using Argos.Entities.Dtos;

namespace Argos.Business.Services.Settings;

/// <summary>Reglas de la configuración editable. Lanzan 422 con un mensaje legible.</summary>
public interface ISettingsValidator
{
    void Validate(SettingsPatchRequest patch);

    /// <summary>
    /// 422 si el modelo no corresponde al proveedor (p. ej. "llama-3.3-70b" con Gemini): antes se guardaba sin aviso y
    /// en tiempo de ejecución se usaba el modelo por defecto (F-10).
    /// </summary>
    void ValidateModelForProvider(string provider, string model);

    /// <summary>strip + lowercase + dedup (conserva el orden); valida formato y tope de destinatarios.</summary>
    IReadOnlyList<string> NormalizeRecipients(IEnumerable<string?> emails);

    /// <summary>Recorta espacios y valida el largo máximo del contexto del entorno (422 si se excede).</summary>
    string NormalizeEnvironmentContext(string text);
}
