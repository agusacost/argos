using Argos.Entities;

namespace Argos.Business.Services.Llm;

/// <summary>
/// Salud pasiva de los proveedores LLM: se alimenta de las llamadas reales (y del chequeo manual), sin pings
/// periódicos. Un fallo saca al eslabón de la cadena durante <c>LLM_COOLDOWN_SECONDS</c>. Vive en memoria.
/// </summary>
public interface ILlmHealthTracker
{
    /// <summary>false mientras dura el cooldown de un fallo reciente.</summary>
    bool IsAvailable(LlmSelection selection);

    /// <summary>Registra el éxito; si venía caído, avisa que se recuperó.</summary>
    Task RecordSuccessAsync(LlmSelection selection, long latencyMs);

    /// <summary>Registra el fallo; si estaba sano o sin datos, avisa que no está disponible.</summary>
    Task RecordFailureAsync(LlmSelection selection, string error, long latencyMs);

    LlmProviderHealth Describe(LlmSelection selection, string role, bool configured);
}
