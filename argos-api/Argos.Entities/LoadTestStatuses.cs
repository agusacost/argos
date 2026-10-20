namespace Argos.Entities;

public static class LoadTestStatuses
{
    public const string Running = "running";
    public const string Passed = "passed";
    public const string ThresholdsFailed = "thresholds_failed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    /// <summary>k6 sale con 99 cuando algún threshold falla; la corrida igual completó.</summary>
    public static string FromExitCode(int exitCode) => exitCode switch
    {
        0 => Passed,
        99 => ThresholdsFailed,
        _ => Failed,
    };
}
