using System.Globalization;
using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Evidence;

/// <summary>
/// Junta la evidencia de la ventana de la alerta partiendo de lo que reportan Nginx o la propia alerta, sin conocer
/// los contenedores del servicio de antemano:
/// <list type="number">
/// <item>Nginx (<c>evidence: nginx</c>): distribución status / upstream_status del access.log, líneas del error.log y
/// el upstream de las respuestas 5xx (<c>upstream_addr</c> y error.log). Docker traduce ese upstream a un contenedor y
/// se agregan sus últimas líneas de log (<c>{job="docker", container="…"}</c>) y su estado.</item>
/// <item>Target (<c>evidence: target</c>): el host del <c>instance</c> de la alerta, traducido a contenedor de la misma
/// forma.</item>
/// </list>
/// </summary>
public sealed class AlertEvidenceService(
    ILokiClient loki,
    IContainerDirectory directory,
    IAlertEvidencePlanner planner,
    INginxUpstreamExtractor upstreamExtractor,
    IAlertEvidenceFormatter formatter,
    IEvidenceConfig config,
    IClock clock,
    ILogger<AlertEvidenceService> log) : IAlertEvidenceService
{
    private const int ErrorLinesFetched = 200;
    private const int ContainerLinesFetched = 50;
    private const string NginxErrorSelector = "{service=\"nginx\", log_type=\"error\"}";

    /// <summary>La condición de la alerta empezó antes del startsAt (cláusula <c>for</c> y ventanas de rate).</summary>
    private static readonly TimeSpan Lookback = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan MaxAfterStart = TimeSpan.FromMinutes(5);

    public async Task<AlertEvidence> CollectAsync(string alertname, IReadOnlyDictionary<string, string>? labels,
        DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        var now = clock.UtcNow;
        var anchor = startsAt is { } s && s <= now ? s : now;
        var start = anchor - Lookback;
        var end = endsAt ?? anchor + MaxAfterStart;
        if (end > now) end = now;
        if (end <= start) end = start + Lookback;

        var plan = planner.PlanFor(alertname, labels);
        if (!config.AlertEvidenceEnabled || plan.IsEmpty)
            return formatter.Build(alertname, start, end, [], [], []);

        IReadOnlyList<NginxStatusCount> status = [];
        IReadOnlyList<string> errors = [];
        IReadOnlyList<string> upstreams = [];
        var hosts = new List<string>(plan.TargetHosts);
        if (plan.IncludeNginx)
        {
            var statusTask = StatusCountsAsync(start, end);
            var errorsTask = LinesAsync(NginxErrorSelector, start, end, ErrorLinesFetched);
            var addrsTask = UpstreamAddrsAsync(start, end);
            status = await statusTask;
            errors = await errorsTask;
            upstreams = upstreamExtractor.Extract(await addrsTask, errors);
            hosts.AddRange(upstreams);
        }

        IReadOnlyList<ResolvedContainer> resolved = hosts.Count == 0 ? [] : await directory.ResolveAsync(hosts);
        var containers = await Task.WhenAll(resolved.Select(async r => new ContainerLogExcerpt(r.Name,
            await LinesAsync($"{{job=\"docker\", container=\"{r.Name}\"}}", start, end, ContainerLinesFetched), r.Status)));

        return formatter.Build(alertname, start, end, status, errors, containers, upstreams);
    }

    private async Task<IReadOnlyList<NginxStatusCount>> StatusCountsAsync(DateTimeOffset start, DateTimeOffset end)
    {
        // 'status' ya es label del stream; upstream_status se extrae del JSON del access.log.
        var query = "sum by (status, upstream_status) (count_over_time({service=\"nginx\", log_type=\"access\"} " +
                    $"| json upstream_status=\"upstream_status\" [{Seconds(start, end)}s]))";
        try
        {
            var samples = await loki.QueryVectorAsync(query, end);
            return samples
                .Select(s => new NginxStatusCount(
                    Label(s.Labels, "status"),
                    Label(s.Labels, "upstream_status"),
                    (long)Math.Round(s.Value)))
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning("[evidence] Loki (status) no disponible: {Error}", ex.Message);
            return [];
        }
    }

    /// <summary>A qué upstream mandó Nginx las requests que terminaron en 5xx.</summary>
    private async Task<IReadOnlyList<(string Addr, long Count)>> UpstreamAddrsAsync(DateTimeOffset start, DateTimeOffset end)
    {
        var query = "sum by (upstream_addr) (count_over_time({service=\"nginx\", log_type=\"access\", status=~\"5..\"} " +
                    $"| json upstream_addr=\"upstream_addr\" | upstream_addr != \"\" [{Seconds(start, end)}s]))";
        try
        {
            var samples = await loki.QueryVectorAsync(query, end);
            return samples
                .Select(s => (Addr: Label(s.Labels, "upstream_addr"), Count: (long)Math.Round(s.Value)))
                .Where(a => a.Addr != "-")
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning("[evidence] Loki (upstream_addr) no disponible: {Error}", ex.Message);
            return [];
        }
    }

    private async Task<IReadOnlyList<string>> LinesAsync(string selector, DateTimeOffset start, DateTimeOffset end, int limit)
    {
        try
        {
            return await loki.QueryLinesAsync(selector, start, end, limit);
        }
        catch (Exception ex)
        {
            log.LogWarning("[evidence] Loki ({Selector}) no disponible: {Error}", selector, ex.Message);
            return [];
        }
    }

    private static string Seconds(DateTimeOffset start, DateTimeOffset end) =>
        Math.Max(60, (long)(end - start).TotalSeconds).ToString(CultureInfo.InvariantCulture);

    private static string Label(IReadOnlyDictionary<string, string> labels, string name) =>
        labels.TryGetValue(name, out var v) && v.Length > 0 && v != "-" ? v : "-";
}
