using System.Globalization;
using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Business.Services.Incidents;

/// <summary>KPIs del dashboard: conteos por estado y tipo, MTTR (promedio, serie y tendencia), volumen por día.</summary>
public sealed class IncidentMetricsCalculator(IClock clock) : IIncidentMetricsCalculator
{
    private static readonly TimeSpan MttrWindow = TimeSpan.FromDays(7);
    private const int MttrSeriesDays = 14;

    // Variaciones menores al 10 % entre ventanas se consideran estables.
    private const double MttrTolerance = 0.10;

    public IncidentMetrics Calculate(IReadOnlyList<Incident> incidents)
    {
        var perDay = CountPerDay(incidents);
        var mttrs = incidents.Where(i => i.MttrMinutes is not null).Select(i => i.MttrMinutes!.Value).ToList();
        var resolutions = Resolutions(incidents);
        return new IncidentMetrics
        {
            TotalIncidents = incidents.Count,
            ByStatus = CountBy(incidents, i => i.Estado),
            ByAlertType = CountBy(incidents, i => i.TipoAlerta),
            MttrAverageMinutes = mttrs.Count > 0 ? RoundingMath.Round(mttrs.Sum() / mttrs.Count, 2) : null,
            IncidentsPerDay = perDay.Select(d => new DailyIncidentCount(d.Key, d.Value)).ToList(),
            RecentTrend = Trend(perDay),
            MttrTrend = MttrTrend(resolutions),
            MttrPerDay = MttrPerDay(resolutions),
        };
    }

    private static Dictionary<string, int> CountBy(IEnumerable<Incident> incidents, Func<Incident, string> key)
    {
        var counts = new Dictionary<string, int>();
        foreach (var incident in incidents)
        {
            var k = key(incident);
            counts[k] = counts.GetValueOrDefault(k) + 1;
        }
        return counts;
    }

    /// <summary>
    /// Incidentes por día de detección, ordenados por fecha. Solo cuentan los que tienen <c>detected_at</c>:
    /// los runbooks no son detecciones y su <c>fecha</c> es la de carga.
    /// </summary>
    private static SortedDictionary<string, int> CountPerDay(IEnumerable<Incident> incidents)
    {
        var perDay = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var incident in incidents)
        {
            if (IsoDateTime.Parse(incident.DetectedAt) is not { } date) continue;
            var day = Day(date);
            perDay[day] = perDay.GetValueOrDefault(day) + 1;
        }
        return perDay;
    }

    /// <summary>Compara el total de la primera y la segunda mitad de la serie por día.</summary>
    private static string Trend(SortedDictionary<string, int> perDay)
    {
        if (perDay.Count < 2) return "stable";
        var counts = perDay.Values.ToList();
        var mid = counts.Count / 2;
        var previous = counts.Take(mid).Sum();
        var recent = counts.Skip(mid).Sum();
        return recent > previous ? "degrading" : recent < previous ? "improving" : "stable";
    }

    /// <summary>(fecha de resolución, MTTR) de los incidentes que tienen ambos.</summary>
    private static List<(DateTimeOffset ResolvedAt, double Mttr)> Resolutions(IEnumerable<Incident> incidents) =>
        incidents
            .Where(i => i.MttrMinutes is not null)
            .Select(i => (Date: IsoDateTime.Parse(i.ResolvedAt), Mttr: i.MttrMinutes!.Value))
            .Where(x => x.Date is not null)
            .Select(x => (x.Date!.Value, x.Mttr))
            .ToList();

    private string MttrTrend(List<(DateTimeOffset ResolvedAt, double Mttr)> resolutions)
    {
        var now = clock.UtcNow;
        var recent = resolutions.Where(r => r.ResolvedAt > now - MttrWindow).Select(r => r.Mttr).ToList();
        var previous = resolutions
            .Where(r => r.ResolvedAt <= now - MttrWindow && r.ResolvedAt > now - 2 * MttrWindow)
            .Select(r => r.Mttr)
            .ToList();
        if (recent.Count == 0 || previous.Count == 0) return "no_data";
        var change = (recent.Average() - previous.Average()) / Math.Max(previous.Average(), 0.01);
        return change > MttrTolerance ? "degrading" : change < -MttrTolerance ? "improving" : "stable";
    }

    private List<DailyMttr> MttrPerDay(List<(DateTimeOffset ResolvedAt, double Mttr)> resolutions)
    {
        var since = clock.UtcNow.AddDays(-MttrSeriesDays);
        return resolutions
            .Where(r => r.ResolvedAt > since)
            .GroupBy(r => Day(r.ResolvedAt))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new DailyMttr(g.Key, RoundingMath.Round(g.Average(r => r.Mttr), 2)))
            .ToList();
    }

    private static string Day(DateTimeOffset date) => date.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
