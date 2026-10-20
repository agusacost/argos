using System.Globalization;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Repository.Http;

namespace Argos.Repository.Observability;

public sealed class PrometheusClient(IHttpClientFactory httpFactory, IObservabilityConfig config) : IPrometheusClient
{
    public async Task<IReadOnlyList<double>> QueryRangeAsync(string expr, DateTimeOffset start, DateTimeOffset end, int stepSeconds)
    {
        var query = new RangeQuery()
            .Add("query", expr)
            .Add("start", IsoDateTime.Format(start.UtcDateTime, aware: true))
            .Add("end", IsoDateTime.Format(end.UtcDateTime, aware: true))
            .Add("step", stepSeconds);
        using var r = await httpFactory.CreateClient(HttpClientNames.Observability)
            .GetAsync($"{config.PrometheusUrl}/api/v1/query_range?{query}");
        r.EnsureSuccessStatusCode();
        var result = JsonNode.Parse(await r.Content.ReadAsStringAsync())?["data"]?["result"]?.AsArray();
        if (result is null || result.Count == 0) return [];
        return (result[0]?["values"]?.AsArray() ?? [])
            .Select(v => double.Parse(v![1]!.GetValue<string>(), CultureInfo.InvariantCulture))
            .ToList();
    }
}
