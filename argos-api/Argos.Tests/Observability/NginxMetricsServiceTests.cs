using Argos.Business.Services.Observability;
using Argos.Common.Time;
using Argos.Repository.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Observability;

/// <summary>M-04: la latencia se pide a Loki como una sola serie (agrupada), no una por combinación de etiquetas.</summary>
public sealed class NginxMetricsServiceTests
{
    [Theory]
    [InlineData("0.50")]
    [InlineData("0.95")]
    [InlineData("0.99")]
    public void QuantileQuery_GroupsIntoASingleSeries(string quantile)
    {
        var query = NginxMetricsService.QuantileQuery(quantile);

        Assert.StartsWith($"quantile_over_time({quantile}, ", query);
        Assert.EndsWith(") by (service)", query);
    }

    [Fact]
    public async Task GetAsync_LatencyQueriesAreGrouped()
    {
        var queries = new List<string>();
        var loki = new Mock<ILokiClient>();
        loki.Setup(l => l.QueryMatrixAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>()))
            .Callback((string q, DateTimeOffset _, DateTimeOffset _, int _) => { lock (queries) queries.Add(q); })
            .ReturnsAsync(new Dictionary<long, double>());
        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

        var metrics = await new NginxMetricsService(loki.Object, clock.Object, NullLogger<NginxMetricsService>.Instance).GetAsync("6h");

        Assert.Equal(360, metrics.StepSeconds);
        var latency = queries.Where(q => q.StartsWith("quantile_over_time", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, latency.Count);
        Assert.All(latency, q => Assert.EndsWith(") by (service)", q));
    }
}
