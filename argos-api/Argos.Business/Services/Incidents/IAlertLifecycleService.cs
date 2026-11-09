using Argos.Entities.Dtos;

namespace Argos.Business.Services.Incidents;

/// <summary>Ciclo de vida de las alertas reales que llegan por n8n: ocurrencias conocidas y resoluciones.</summary>
public interface IAlertLifecycleService
{
    /// <summary>
    /// Registra una alerta ya documentada como incidente abierto ('pendiente') con la solución del mejor match
    /// de la base. Si esa ocurrencia (alerta + startsAt) ya existe, no hace nada.
    /// </summary>
    Task<AlertOccurrenceResponse> RegisterOccurrenceAsync(AlertOccurrenceRequest request);

    /// <summary>Cierra el incidente abierto de la alerta: <c>resolved_at</c> = endsAt y MTTR real.</summary>
    Task<AlertResolutionResponse> ResolveAsync(AlertResolutionRequest request);
}
