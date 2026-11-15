using System.Text.RegularExpressions;

namespace Argos.Business.Services.Evidence;

/// <summary>
/// Junta los upstream de dos fuentes de Nginx: <c>upstream_addr</c> del access.log ("172.20.0.12:8080", o varios
/// separados por coma si Nginx reintentó) y el error.log (<c>upstream: "http://172.20.0.12:8080/…"</c>, o
/// <c>mi-api could not be resolved</c> cuando el contenedor no existe en el DNS). Devuelve hosts sin puerto,
/// del más frecuente al menos frecuente, como mucho <see cref="MaxUpstreams"/>.
/// </summary>
public sealed partial class NginxUpstreamExtractor : INginxUpstreamExtractor
{
    public const int MaxUpstreams = 2;

    public IReadOnlyList<string> Extract(IReadOnlyList<(string Addr, long Count)> upstreamAddrs, IReadOnlyList<string> errorLines)
    {
        var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        void Add(string host, long n)
        {
            host = host.Trim().Trim('"');
            if (host.Length == 0 || host == "-") return;
            counts[host] = counts.TryGetValue(host, out var c) ? c + n : n;
        }

        foreach (var (addr, count) in upstreamAddrs)
            foreach (var part in (addr ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                Add(AlertEvidencePlanner.HostOf(part), Math.Max(1, count));

        foreach (var line in errorLines)
        {
            var upstream = UpstreamUrl().Match(line);
            if (upstream.Success) Add(upstream.Groups["host"].Value, 1);
            var unresolved = NotResolved().Match(line);
            if (unresolved.Success) Add(unresolved.Groups["host"].Value, 1);
        }

        return counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(MaxUpstreams).Select(kv => kv.Key).ToList();
    }

    [GeneratedRegex(@"upstream: ""[a-z]+://(?<host>[^/"":\]]+)")]
    private static partial Regex UpstreamUrl();

    [GeneratedRegex(@"\] \d+#\d+: (?:\*\d+ )?(?<host>[A-Za-z0-9._-]+) could not be resolved")]
    private static partial Regex NotResolved();
}
