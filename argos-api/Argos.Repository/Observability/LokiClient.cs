using System.Globalization;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Repository.Http;

namespace Argos.Repository.Observability;

public sealed class LokiClient(IHttpClientFactory httpFactory, IObservabilityConfig config) : ILokiClient
{
    public async Task<IReadOnlyList<string>> QueryLinesAsync(string selector, DateTimeOffset start, DateTimeOffset end, int limit)
    {
        var query = new RangeQuery()
            .Add("query", selector)
            .Add("start", RangeQuery.UnixNanos(start))
            .Add("end", RangeQuery.UnixNanos(end))
            .Add("limit", limit)
            .Add("direction", "backward");
        var data = await GetAsync(query);
        var lines = new List<string>();
        foreach (var stream in data?["data"]?["result"]?.AsArray() ?? [])
            foreach (var v in stream?["values"]?.AsArray() ?? [])
                lines.Add(v![1]!.GetValue<string>());
        return lines;
    }

    public async Task<IReadOnlyDictionary<long, double>> QueryMatrixAsync(string query, DateTimeOffset start, DateTimeOffset end,
        int stepSeconds)
    {
        var q = new RangeQuery()
            .Add("query", query)
            .Add("start", RangeQuery.UnixNanos(start))
            .Add("end", RangeQuery.UnixNanos(end))
            .Add("step", stepSeconds);
        var result = (await GetAsync(q))?["data"]?["result"]?.AsArray();
        var output = new Dictionary<long, double>();
        if (result is null || result.Count == 0) return output;
        foreach (var v in result[0]?["values"]?.AsArray() ?? [])
        {
            if (double.TryParse(v?[0]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var ts) &&
                double.TryParse(v?[1]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                output[(long)ts] = val;
        }
        return output;
    }

    private async Task<JsonNode?> GetAsync(RangeQuery query)
    {
        using var r = await httpFactory.CreateClient(HttpClientNames.Observability)
            .GetAsync($"{config.LokiUrl}/loki/api/v1/query_range?{query}");
        r.EnsureSuccessStatusCode();
        return JsonNode.Parse(await r.Content.ReadAsStringAsync());
    }
}
