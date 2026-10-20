using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Observability;

/// <summary>
/// Golden signals de Nginx desde Loki: requests por clase de status, latencia upstream P50/P95/P99 y tasa
/// de error 5xx, alineadas a una grilla temporal común. Las queries corren en paralelo.
/// </summary>
public sealed class NginxMetricsService(ILokiClient loki, IClock clock, ILogger<NginxMetricsService> log) : INginxMetricsService
{
    private const string RateWindow = "2m";
    private const string LatencyStream =
        "{service=\"nginx\", log_type=\"access\"} | json | upstream_response_time != \"-\" | unwrap upstream_response_time";

    private static readonly Dictionary<string, int> RangeSeconds = new()
    {
        ["15m"] = 15 * 60, ["1h"] = 3600, ["3h"] = 3 * 3600, ["6h"] = 6 * 3600,
    };

    public async Task<NginxMetrics> GetAsync(string range)
    {
        var window = RangeSeconds.GetValueOrDefault(range, 3600);
        var now = clock.UtcNow;
        var start = now.AddSeconds(-window);
        var step = Math.Max(30, window / 60);  // ~60 puntos

        // Loki alinea los samples a múltiplos absolutos del step: la grilla se alinea igual.
        var grid = new List<long>();
        for (var t = RoundingMath.FloorDiv(start.ToUnixTimeSeconds(), step) * step; t < now.ToUnixTimeSeconds() + 1; t += step)
            grid.Add(t);

        var s2 = SeriesAsync(StatusQuery("2.."), grid, start, now, step, 0.0);
        var s3 = SeriesAsync(StatusQuery("3.."), grid, start, now, step, 0.0);
        var s4 = SeriesAsync(StatusQuery("4.."), grid, start, now, step, 0.0);
        var s5 = SeriesAsync(StatusQuery("5.."), grid, start, now, step, 0.0);
        var p50 = SeriesAsync(QuantileQuery("0.50"), grid, start, now, step, null);
        var p95 = SeriesAsync(QuantileQuery("0.95"), grid, start, now, step, null);
        var p99 = SeriesAsync(QuantileQuery("0.99"), grid, start, now, step, null);

        var status = new[] { await s2, await s3, await s4, await s5 };
        var total = grid.Select((_, i) => status.Sum(s => s[i] ?? 0.0)).ToList();
        var errorRate = grid.Select((_, i) =>
            total[i] > 0 ? RoundingMath.Round((status[3][i] ?? 0.0) / total[i] * 100, 2) : 0.0).ToList();

        return new NginxMetrics
        {
            Range = range,
            StepSeconds = step,
            Timestamps = grid,
            StatusSeries = new NginxStatusSeries
            {
                Status2xx = status[0], Status3xx = status[1], Status4xx = status[2], Status5xx = status[3],
            },
            Latency = new NginxLatencySeries { P50 = await p50, P95 = await p95, P99 = await p99 },
            ErrorRate = errorRate.Select(x => (double?)x).ToList(),
            RequestRate = total.Select(x => (double?)x).ToList(),
        };
    }

    private static string StatusQuery(string statusClass) =>
        "sum(rate({service=\"nginx\", log_type=\"access\", status=~\"" + statusClass + "\"}[" + RateWindow + "]))";

    private static string QuantileQuery(string quantile) => $"quantile_over_time({quantile}, {LatencyStream} [{RateWindow}])";

    /// <summary>Valores de la query sobre la grilla (tolerando un step de desfase); sin dato → <paramref name="fallback"/>.</summary>
    private async Task<IReadOnlyList<double?>> SeriesAsync(string query, IReadOnlyList<long> grid,
        DateTimeOffset start, DateTimeOffset end, int step, double? fallback)
    {
        IReadOnlyDictionary<long, double> raw;
        try
        {
            raw = await loki.QueryMatrixAsync(query, start, end, step);
        }
        catch (Exception ex)
        {
            log.LogWarning("[observability] Loki range error: {Error}", ex.Message);
            raw = new Dictionary<long, double>();
        }
        if (raw.Count == 0) return grid.Select(_ => fallback).ToList();
        return grid.Select(t =>
            raw.TryGetValue(t, out var v) || raw.TryGetValue(t - step, out v) || raw.TryGetValue(t + step, out v)
                ? v
                : fallback).ToList();
    }
}
