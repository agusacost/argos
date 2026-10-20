using System.Globalization;
using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Business.Services.Incidents;

/// <summary>KPIs del dashboard: conteos por estado y tipo, MTTR promedio, serie por día y tendencia.</summary>
public sealed class IncidentMetricsCalculator : IIncidentMetricsCalculator
{
    public IncidentMetrics Calculate(IReadOnlyList<Incident> incidents)
    {
        var perDay = CountPerDay(incidents);
        var mttrs = incidents.Where(i => i.MttrMinutes is not null).Select(i => i.MttrMinutes!.Value).ToList();
        return new IncidentMetrics
        {
            TotalIncidents = incidents.Count,
            ByStatus = CountBy(incidents, i => i.Estado),
            ByAlertType = CountBy(incidents, i => i.TipoAlerta),
            MttrAverageMinutes = mttrs.Count > 0 ? RoundingMath.Round(mttrs.Sum() / mttrs.Count, 2) : null,
            IncidentsPerDay = perDay.Select(d => new DailyIncidentCount(d.Key, d.Value)).ToList(),
            RecentTrend = Trend(perDay),
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

    /// <summary>Incidentes por día (según detected_at o fecha), ordenados por fecha.</summary>
    private static SortedDictionary<string, int> CountPerDay(IEnumerable<Incident> incidents)
    {
        var perDay = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var incident in incidents)
        {
            if (IsoDateTime.Parse(incident.ReferenceDate) is not { } date) continue;
            var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
}
