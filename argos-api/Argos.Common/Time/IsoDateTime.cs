using System.Globalization;

namespace Argos.Common.Time;

/// <summary>
/// Formato y parseo de las fechas ISO 8601 guardadas en Qdrant (las consumen el dashboard y n8n).
/// </summary>
public static class IsoDateTime
{
    /// <summary>ISO 8601 con microsegundos solo si son distintos de cero.</summary>
    public static string Format(DateTime utc, bool aware)
    {
        var micro = (utc.Ticks % TimeSpan.TicksPerSecond) / 10;
        var s = utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        if (micro != 0) s += "." + micro.ToString("D6", CultureInfo.InvariantCulture);
        return aware ? s + "+00:00" : s;
    }

    /// <summary>
    /// Parser tolerante (acepta offset, sufijo <c>Z</c> o ninguno). Los timestamps sin offset se
    /// interpretan como UTC, así se pueden restar fechas con y sin zona.
    /// </summary>
    public static DateTimeOffset? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var dto) ? dto : null;
    }
}
