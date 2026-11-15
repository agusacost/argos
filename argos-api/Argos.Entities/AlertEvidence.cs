namespace Argos.Entities;

/// <summary>
/// Muestra acotada de logs de la ventana de una alerta (respuesta de <c>POST /internal/evidence</c>). <c>text</c> es
/// la sección lista para pegar en el prompt (vacía si no hay evidencia). Las IP van tal cual: se seudonimizan en el
/// camino de generación (<c>/internal/llm/generate</c>), no acá.
/// </summary>
public sealed class AlertEvidence
{
    public string Alertname { get; init; } = "";
    public string WindowStart { get; init; } = "";
    public string WindowEnd { get; init; } = "";

    /// <summary>Hay al menos una línea de evidencia.</summary>
    public bool Available { get; init; }

    /// <summary>Líneas que entraron en <see cref="Text"/> (sin contar encabezados).</summary>
    public int LineCount { get; init; }

    public IReadOnlyList<NginxStatusCount> StatusCounts { get; init; } = [];
    public IReadOnlyList<string> NginxErrors { get; init; } = [];
    /// <summary>Hosts que Nginx reportó como upstream de las respuestas 5xx (IP o nombre, sin puerto).</summary>
    public IReadOnlyList<string> Upstreams { get; init; } = [];

    public IReadOnlyList<ContainerLogExcerpt> ContainerLogs { get; init; } = [];
    public string Text { get; init; } = "";
}
