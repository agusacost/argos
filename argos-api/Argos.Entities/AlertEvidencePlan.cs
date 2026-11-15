namespace Argos.Entities;

/// <summary>
/// De dónde sale la evidencia de una alerta. <see cref="IncludeNginx"/>: logs de Nginx, y a partir de ellos el
/// upstream que Nginx reporta como origen de los errores. <see cref="TargetHosts"/>: hosts que nombra la propia alerta
/// (el <c>instance</c> de Prometheus), para alertas sobre un componente puntual. Ningún nombre de contenedor sale de
/// Argos: los dos caminos parten de lo que reportan Nginx o la alerta.
/// </summary>
public sealed record AlertEvidencePlan(bool IncludeNginx, IReadOnlyList<string> TargetHosts)
{
    public static readonly AlertEvidencePlan Empty = new(false, []);

    public bool IsEmpty => !IncludeNginx && TargetHosts.Count == 0;
}
