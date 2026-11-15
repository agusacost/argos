namespace Argos.Common.Configuration;

/// <summary>Protección de datos personales en el texto que sale hacia los proveedores LLM externos.</summary>
public interface IPrivacyConfig
{
    /// <summary>
    /// LLM_PSEUDONYMIZE_IPS (default true): las direcciones IP se reemplazan por marcadores antes de llamar al
    /// proveedor y se restauran en la respuesta (Ley 25.326, art. 12: transferencia internacional).
    /// </summary>
    bool PseudonymizeIps { get; }
}
