using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Business.Services.Evidence;

/// <summary>
/// Presupuesto fijo de hasta 18 líneas (6 de distribución de status, 6 del error.log agrupadas y 6 de contenedores,
/// 3 por contenedor), cada una truncada a 200 caracteres: el prompt no crece con el volumen de logs. Los contenedores
/// son los que surgen de Nginx (upstream de los errores) o de la alerta (<c>instance</c>); un contenedor sin líneas en
/// la ventana igual aparece, con su estado según Docker, en una sola línea.
/// </summary>
public sealed partial class AlertEvidenceFormatter : IAlertEvidenceFormatter
{
    public const int MaxStatusLines = 6;
    public const int MaxNginxErrorLines = 6;
    public const int MaxContainerLines = 6;
    public const int MaxLinesPerContainer = 3;
    public const int MaxLineChars = 200;
    public const int MaxTotalLines = MaxStatusLines + MaxNginxErrorLines + MaxContainerLines;

    public AlertEvidence Build(string alertname, DateTimeOffset start, DateTimeOffset end,
        IReadOnlyList<NginxStatusCount> statusCounts,
        IReadOnlyList<string> nginxErrors,
        IReadOnlyList<ContainerLogExcerpt> containerLines,
        IReadOnlyList<string>? upstreams = null)
    {
        upstreams ??= [];
        var status = statusCounts
            .Where(s => s.Count > 0)
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Status, StringComparer.Ordinal)
            .Take(MaxStatusLines)
            .ToList();
        var errors = GroupErrors(nginxErrors);
        var containers = SelectContainerLines(containerLines);

        var lineCount = status.Count + errors.Count + containers.Sum(c => c.Lines.Count);
        return new AlertEvidence
        {
            Alertname = alertname,
            WindowStart = IsoDateTime.Format(start.UtcDateTime, aware: true),
            WindowEnd = IsoDateTime.Format(end.UtcDateTime, aware: true),
            Available = lineCount > 0,
            LineCount = lineCount,
            StatusCounts = status,
            NginxErrors = errors,
            Upstreams = upstreams,
            ContainerLogs = containers,
            Text = lineCount == 0 ? "" : Render(start, end, status, errors, upstreams, containers),
        };
    }

    /// <summary>
    /// Agrupa los mensajes repetidos del error.log (mismo mensaje con otro request/cliente) y muestra el más reciente
    /// de cada grupo con la cantidad: "connect() failed ... (×42)". Ordena por cantidad.
    /// </summary>
    private static List<string> GroupErrors(IReadOnlyList<string> lines)
    {
        var groups = new Dictionary<string, (string Latest, int Count, int FirstIndex)>(StringComparer.Ordinal);
        for (var i = 0; i < lines.Count; i++)
        {
            var display = ErrorPrefix().Replace(lines[i].Trim(), "[${level}] ");
            if (display.Length == 0) continue;
            var key = ErrorDetails().Replace(display, "");
            groups[key] = groups.TryGetValue(key, out var g) ? (g.Latest, g.Count + 1, g.FirstIndex) : (display, 1, i);
        }
        return groups.Values
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.FirstIndex)
            .Take(MaxNginxErrorLines)
            .Select(g => g.Count > 1
                ? Truncate(g.Latest, MaxLineChars - 8) + $" (×{g.Count.ToString(CultureInfo.InvariantCulture)})"
                : Truncate(g.Latest, MaxLineChars))
            .ToList();
    }

    /// <summary>Por contenedor, las líneas más nuevas con pinta de error; si no hay, las más nuevas a secas.</summary>
    private static List<ContainerLogExcerpt> SelectContainerLines(IReadOnlyList<ContainerLogExcerpt> containers)
    {
        var result = new List<ContainerLogExcerpt>();
        var budget = MaxContainerLines;
        foreach (var container in containers)
        {
            if (budget <= 0) break;
            var distinct = container.Lines
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var take = Math.Min(MaxLinesPerContainer, budget);
            var errorLike = distinct.Where(l => ErrorLike().IsMatch(l)).Take(take).ToList();
            var chosen = errorLike.Count > 0 ? errorLike : distinct.Take(take).ToList();
            if (chosen.Count == 0 && container.Status is { Length: > 0 }) chosen = [NoLinesMarker];
            if (chosen.Count == 0) continue;
            budget -= chosen.Count;
            result.Add(new ContainerLogExcerpt(container.Container, chosen.Select(l => Truncate(l, MaxLineChars)).ToList(),
                container.Status));
        }
        return result;
    }

    private static string Render(DateTimeOffset start, DateTimeOffset end, IReadOnlyList<NginxStatusCount> status,
        IReadOnlyList<string> errors, IReadOnlyList<string> upstreams, IReadOnlyList<ContainerLogExcerpt> containers)
    {
        var sb = new StringBuilder();
        sb.Append("Evidencia de logs (Loki, ventana ")
            .Append(start.UtcDateTime.ToString("HH:mm", CultureInfo.InvariantCulture)).Append('–')
            .Append(end.UtcDateTime.ToString("HH:mm", CultureInfo.InvariantCulture))
            .Append(" UTC; muestra acotada, puede no incluir todo):\n");
        if (status.Count > 0)
        {
            sb.Append("Nginx access.log, requests por status (upstream = respuesta del upstream; \"-\" = no llegó al upstream):\n");
            foreach (var s in status)
                sb.Append("- ").Append(s.Status).Append(" (upstream ").Append(s.UpstreamStatus).Append("): ")
                    .Append(s.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        if (errors.Count > 0)
        {
            sb.Append("Nginx error.log (mensajes agrupados, el más reciente de cada uno):\n");
            foreach (var e in errors) sb.Append("- ").Append(e).Append('\n');
        }
        if (upstreams.Count > 0)
            sb.Append("Upstream que Nginx reporta en los errores: ").Append(string.Join(", ", upstreams)).Append('\n');
        if (containers.Count > 0)
        {
            sb.Append("Contenedores identificados a partir de Nginx o de la alerta (estado según Docker; últimas líneas relevantes):\n");
            foreach (var c in containers)
                foreach (var l in c.Lines)
                {
                    sb.Append("- ").Append(c.Container);
                    if (c.Status is { Length: > 0 }) sb.Append(" [").Append(c.Status).Append(']');
                    sb.Append(": ").Append(l).Append('\n');
                }
        }
        return sb.ToString().TrimEnd();
    }

    public const string NoLinesMarker = "(sin líneas de log en la ventana)";

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    // "2026/11/11 10:01:02 [error] 29#29: *123 " → "[error] "
    [GeneratedRegex(@"^(?:\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} )?\[(?<level>\w+)\] \d+#\d+: (?:\*\d+ )?")]
    private static partial Regex ErrorPrefix();

    // Lo que cambia entre repeticiones del mismo error: cliente, request, upstream concreto, host, referrer.
    [GeneratedRegex(@", (?:client|server|request|upstream|host|referrer): (?:""[^""]*""|[^,]*)")]
    private static partial Regex ErrorDetails();

    [GeneratedRegex(@"(?i)\b(error|fail\w*|exception|refused|timeout|timed out|fatal|crit\w*|unable|cannot|denied|shutting down|stopp\w*|unhealthy)\b")]
    private static partial Regex ErrorLike();
}
