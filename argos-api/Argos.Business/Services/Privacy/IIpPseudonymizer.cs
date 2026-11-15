namespace Argos.Business.Services.Privacy;

/// <summary>
/// Seudonimización de direcciones IP en el texto que se envía a un proveedor LLM externo (Ley 25.326, art. 12).
/// Cada llamada al LLM abre su propia sesión: los marcadores son estables dentro de la llamada y se restauran en la
/// respuesta, así el operador ve las IP reales en el email y en el documento.
/// </summary>
public interface IIpPseudonymizer
{
    /// <summary>Sesión nueva; si LLM_PSEUDONYMIZE_IPS=false, una sesión que no modifica el texto.</summary>
    IpPseudonymizationSession Begin();
}
