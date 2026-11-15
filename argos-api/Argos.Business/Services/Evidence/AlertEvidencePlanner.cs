using Argos.Entities;

namespace Argos.Business.Services.Evidence;

/// <summary>
/// La fuente la declara la regla de alerta con el label <c>evidence</c>, no Argos:
/// <list type="bullet">
/// <item><c>evidence: nginx</c> (reglas sobre el tráfico HTTP): logs de Nginx y, a partir de ellos, el upstream que
/// Nginx reporta como origen de los errores.</item>
/// <item><c>evidence: target</c> (un componente que dejó de responder): el host del label <c>instance</c>.</item>
/// <item>sin label: sin evidencia (por ejemplo, las alertas de recursos del host).</item>
/// </list>
/// Respaldo cuando no hay labels (documentar desde el panel, que solo conoce el tipo de alerta): las alertas cuyo
/// nombre empieza con "Nginx" usan la evidencia de Nginx. Argos no conoce los nombres de los contenedores del servicio.
/// </summary>
public sealed class AlertEvidencePlanner : IAlertEvidencePlanner
{
    public const string EvidenceLabel = "evidence";
    public const string NginxSource = "nginx";
    public const string TargetSource = "target";

    public AlertEvidencePlan PlanFor(string alertname, IReadOnlyDictionary<string, string>? labels)
    {
        if (labels is not null && labels.TryGetValue(EvidenceLabel, out var source))
        {
            if (string.Equals(source, NginxSource, StringComparison.OrdinalIgnoreCase))
                return new AlertEvidencePlan(true, []);
            if (string.Equals(source, TargetSource, StringComparison.OrdinalIgnoreCase))
                return labels.TryGetValue("instance", out var instance) && HostOf(instance) is { Length: > 0 } host
                    ? new AlertEvidencePlan(false, [host])
                    : AlertEvidencePlan.Empty;
            return AlertEvidencePlan.Empty;
        }
        if (labels is null && (alertname ?? "").Trim().StartsWith("Nginx", StringComparison.OrdinalIgnoreCase))
            return new AlertEvidencePlan(true, []);
        return AlertEvidencePlan.Empty;
    }

    /// <summary>"node-exporter:9100" → "node-exporter"; "[::1]:9100" → "::1".</summary>
    public static string HostOf(string instance)
    {
        var s = (instance ?? "").Trim();
        if (s.StartsWith('['))
        {
            var close = s.IndexOf(']');
            return close > 1 ? s[1..close] : "";
        }
        var colon = s.LastIndexOf(':');
        return colon > 0 && s.IndexOf(':') == colon ? s[..colon] : s;
    }
}
