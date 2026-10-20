namespace Argos.Common.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public string UtcNowNaive() => IsoDateTime.Format(DateTime.UtcNow, aware: false);

    public string UtcNowAware() => IsoDateTime.Format(DateTime.UtcNow, aware: true);
}
