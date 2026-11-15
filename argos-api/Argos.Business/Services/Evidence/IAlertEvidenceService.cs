using Argos.Entities;

namespace Argos.Business.Services.Evidence;

/// <summary>
/// Evidencia de logs de la ventana de una alerta (Loki) para el prompt del LLM. Nunca falla: si Loki o Docker no
/// responden, o no hay líneas, devuelve evidencia vacía y el flujo sigue como antes.
/// </summary>
public interface IAlertEvidenceService
{
    /// <param name="alertname">Tipo de alerta.</param>
    /// <param name="labels">Labels de la alerta (<c>evidence</c>, <c>instance</c>); null si no se conocen.</param>
    /// <param name="startsAt"><c>startsAt</c> de Alertmanager; null = ahora.</param>
    /// <param name="endsAt">Resolución, si la hubo; null = la ventana termina ahora (como mucho 5 min después del inicio).</param>
    Task<AlertEvidence> CollectAsync(string alertname, IReadOnlyDictionary<string, string>? labels,
        DateTimeOffset? startsAt, DateTimeOffset? endsAt);
}
