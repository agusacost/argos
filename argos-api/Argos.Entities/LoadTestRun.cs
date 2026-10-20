using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>Corrida de k6 lanzada desde el dashboard (colección <c>argos_loadtests</c>).</summary>
public sealed class LoadTestRun : Entity
{
    public string Scenario { get; set; } = "";
    public string ScenarioName { get; set; } = "";
    public string Status { get; set; } = LoadTestStatuses.Running;
    public string BaseUrl { get; set; } = "";
    public string StartedAt { get; set; } = "";
    public string? FinishedAt { get; set; }
    public int? ExitCode { get; set; }
    public LoadTestSummary? Summary { get; set; }
    public string? LogTail { get; set; }
    public string? ContainerId { get; set; }

    [JsonIgnore]
    public bool HasContainer => !string.IsNullOrEmpty(ContainerId);

    public void AttachContainer(string containerId) => ContainerId = containerId;

    public void Complete(int exitCode, LoadTestSummary? summary, string logTail, string finishedAt)
    {
        Status = LoadTestStatuses.FromExitCode(exitCode);
        ExitCode = exitCode;
        Summary = summary;
        LogTail = logTail;
        FinishedAt = finishedAt;
    }

    public void Fail(string reason, string finishedAt)
    {
        Status = LoadTestStatuses.Failed;
        LogTail = reason;
        FinishedAt = finishedAt;
    }

    public void Cancel(string finishedAt)
    {
        Status = LoadTestStatuses.Cancelled;
        FinishedAt = finishedAt;
    }
}
