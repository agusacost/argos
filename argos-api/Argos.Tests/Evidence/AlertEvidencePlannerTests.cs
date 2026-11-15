using Argos.Business.Services.Evidence;

namespace Argos.Tests.Evidence;

/// <summary>La fuente de la evidencia la declara la regla (label <c>evidence</c>), no una lista de contenedores.</summary>
public sealed class AlertEvidencePlannerTests
{
    private static Dictionary<string, string> Labels(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void NginxSource_UsesNginxAndNoContainerNames()
    {
        var plan = new AlertEvidencePlanner().PlanFor("NginxCriticalErrorRate", Labels(("evidence", "nginx")));

        Assert.True(plan.IncludeNginx);
        Assert.Empty(plan.TargetHosts);
    }

    [Theory]
    [InlineData("node-exporter:9100", "node-exporter")]
    [InlineData("nginx-exporter:9113", "nginx-exporter")]
    [InlineData("cadvisor", "cadvisor")]
    [InlineData("[::1]:9100", "::1")]
    public void TargetSource_UsesTheHostOfTheInstance(string instance, string host)
    {
        var plan = new AlertEvidencePlanner().PlanFor("NodeExporterDown",
            Labels(("evidence", "target"), ("instance", instance)));

        Assert.False(plan.IncludeNginx);
        Assert.Equal([host], plan.TargetHosts);
    }

    [Fact]
    public void TargetSource_WithoutInstance_IsEmpty()
    {
        Assert.True(new AlertEvidencePlanner().PlanFor("NginxDown", Labels(("evidence", "target"))).IsEmpty);
    }

    [Theory]
    [InlineData("HostCriticalCPU")]
    [InlineData("NginxCriticalErrorRate")]
    public void LabelsWithoutEvidence_AreEmpty(string alertname)
    {
        Assert.True(new AlertEvidencePlanner().PlanFor(alertname, Labels(("severity", "critical"))).IsEmpty);
    }

    [Theory]
    [InlineData("NginxCriticalErrorRate", true)]
    [InlineData("HostCriticalCPU", false)]
    public void WithoutLabels_FallsBackToNginxForNginxAlerts(string alertname, bool nginx)
    {
        var plan = new AlertEvidencePlanner().PlanFor(alertname, null);

        Assert.Equal(nginx, plan.IncludeNginx);
        Assert.Empty(plan.TargetHosts);
    }
}
