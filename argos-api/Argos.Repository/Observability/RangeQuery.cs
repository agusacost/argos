using System.Globalization;

namespace Argos.Repository.Observability;

/// <summary>Query string de los endpoints <c>query_range</c> de Loki y Prometheus.</summary>
public sealed class RangeQuery
{
    private readonly List<KeyValuePair<string, string>> _params = [];

    public RangeQuery Add(string key, string value)
    {
        _params.Add(new(key, value));
        return this;
    }

    public RangeQuery Add(string key, long value) => Add(key, value.ToString(CultureInfo.InvariantCulture));

    public override string ToString() =>
        string.Join("&", _params.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

    /// <summary>Nanosegundos desde epoch (formato de Loki).</summary>
    public static long UnixNanos(DateTimeOffset t) => (t.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100;
}
