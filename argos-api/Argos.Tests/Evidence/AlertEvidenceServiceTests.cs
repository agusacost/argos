using Argos.Business.Services.Evidence;
using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Evidence;

public sealed class AlertEvidenceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 10, 3, 0, TimeSpan.Zero);

    private static readonly Dictionary<string, string> NginxLabels = new()
    {
        ["alertname"] = "NginxCriticalErrorRate", ["severity"] = "critical", ["evidence"] = "nginx",
    };

    private static AlertEvidenceService Create(Mock<ILokiClient> loki, Mock<IContainerDirectory>? directory = null,
        bool enabled = true)
    {
        var config = new Mock<IEvidenceConfig>();
        config.SetupGet(c => c.AlertEvidenceEnabled).Returns(enabled);
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);
        directory ??= new Mock<IContainerDirectory>();
        return new AlertEvidenceService(loki.Object, directory.Object, new AlertEvidencePlanner(),
            new NginxUpstreamExtractor(), new AlertEvidenceFormatter(), config.Object, clock.Object,
            NullLogger<AlertEvidenceService>.Instance);
    }

    [Fact]
    public async Task LokiDown_ReturnsEmptyEvidenceWithoutFailing()
    {
        var loki = new Mock<ILokiClient>();
        loki.Setup(l => l.QueryLinesAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));
        loki.Setup(l => l.QueryVectorAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var evidence = await Create(loki).CollectAsync("NginxCriticalErrorRate", NginxLabels, Now.AddMinutes(-1), null);

        Assert.False(evidence.Available);
        Assert.Equal("", evidence.Text);
    }

    [Fact]
    public async Task Nginx_FollowsTheUpstreamThatNginxReports()
    {
        // F2: la API responde 500; Nginx registra a qué upstream mandó esas requests.
        var selectors = new List<string>();
        var loki = new Mock<ILokiClient>();
        loki.Setup(l => l.QueryLinesAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .Callback<string, DateTimeOffset, DateTimeOffset, int>((s, start, end, _) =>
            {
                lock (selectors) selectors.Add(s);
                Assert.Equal(Now.AddMinutes(-11), start);
                Assert.Equal(Now, end);
            })
            .ReturnsAsync((string s, DateTimeOffset _, DateTimeOffset _, int _) => s.Contains("app-svc")
                ? ["System.Exception: Error al obtener usuarios: A network-related error occurred while establishing a connection to SQL Server"]
                : []);
        loki.Setup(l => l.QueryVectorAsync(It.Is<string>(q => q.Contains("by (upstream_addr)")), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync([new LokiSample(new Dictionary<string, string> { ["upstream_addr"] = "172.20.0.23:8080" }, 221)]);
        loki.Setup(l => l.QueryVectorAsync(It.Is<string>(q => q.Contains("by (status, upstream_status)")), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync([new LokiSample(new Dictionary<string, string> { ["status"] = "500", ["upstream_status"] = "500" }, 221)]);
        var directory = new Mock<IContainerDirectory>();
        directory.Setup(d => d.ResolveAsync(It.Is<IReadOnlyCollection<string>>(h => h.SequenceEqual(new[] { "172.20.0.23" }))))
            .ReturnsAsync([new ResolvedContainer("172.20.0.23", "app-svc", "Up 2 hours")]);

        var evidence = await Create(loki, directory).CollectAsync("NginxCriticalErrorRate", NginxLabels, Now.AddMinutes(-1), null);

        Assert.Equal(["172.20.0.23"], evidence.Upstreams);
        Assert.Contains("{job=\"docker\", container=\"app-svc\"}", selectors);
        Assert.Contains("- 500 (upstream 500): 221", evidence.Text);
        Assert.Contains("Upstream que Nginx reporta en los errores: 172.20.0.23", evidence.Text);
        Assert.Contains("- app-svc [Up 2 hours]: System.Exception", evidence.Text);
    }

    [Fact]
    public async Task Nginx_WithoutAnUpstreamInTheLogs_DoesNotLookAtAnyContainer()
    {
        var loki = new Mock<ILokiClient>();
        loki.Setup(l => l.QueryLinesAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .ReturnsAsync([]);
        loki.Setup(l => l.QueryVectorAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>())).ReturnsAsync([]);
        var directory = new Mock<IContainerDirectory>(MockBehavior.Strict);

        var evidence = await Create(loki, directory).CollectAsync("NginxCriticalErrorRate", NginxLabels, Now, null);

        Assert.Empty(evidence.ContainerLogs);
        directory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Target_UsesTheInstanceOfTheAlertAndItsDockerState()
    {
        var loki = new Mock<ILokiClient>(MockBehavior.Strict);
        loki.Setup(l => l.QueryLinesAsync("{job=\"docker\", container=\"node-exporter\"}", It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(), It.IsAny<int>())).ReturnsAsync([]);
        var directory = new Mock<IContainerDirectory>();
        directory.Setup(d => d.ResolveAsync(It.Is<IReadOnlyCollection<string>>(h => h.SequenceEqual(new[] { "node-exporter" }))))
            .ReturnsAsync([new ResolvedContainer("node-exporter", "node-exporter", "Exited (0) 2 minutes ago")]);
        var labels = new Dictionary<string, string>
        {
            ["alertname"] = "NodeExporterDown", ["evidence"] = "target", ["instance"] = "node-exporter:9100",
        };

        var evidence = await Create(loki, directory).CollectAsync("NodeExporterDown", labels, Now, null);

        Assert.Contains("- node-exporter [Exited (0) 2 minutes ago]: " + AlertEvidenceFormatter.NoLinesMarker, evidence.Text);
        Assert.Equal(1, evidence.LineCount);
    }

    [Fact]
    public async Task Disabled_DoesNotCallLoki()
    {
        var loki = new Mock<ILokiClient>(MockBehavior.Strict);

        var evidence = await Create(loki, enabled: false).CollectAsync("NginxCriticalErrorRate", NginxLabels, Now, null);

        Assert.False(evidence.Available);
    }
}
