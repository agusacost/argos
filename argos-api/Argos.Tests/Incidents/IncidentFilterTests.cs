using Argos.Entities;

namespace Argos.Tests.Incidents;

public sealed class IncidentFilterTests
{
    private static readonly IncidentSummary Incident = new()
    {
        TipoAlerta = "NginxCriticalErrorRate",
        Resumen = "Tasa de errores 5xx > 20% — servicio degradado",
    };

    [Theory]
    [InlineData("Nginx")]
    [InlineData("nginx")]
    [InlineData("errorrate")]
    [InlineData("NginxCriticalErrorRate")]
    [InlineData(" critical ")]
    [InlineData(null)]
    [InlineData("")]
    public void TipoContains_MatchesPartialCaseInsensitive(string? tipo) =>
        Assert.True(new IncidentFilter { TipoContains = tipo }.MatchesText(Incident));

    [Fact]
    public void TipoContains_RejectsOtherTypes() =>
        Assert.False(new IncidentFilter { TipoContains = "HostHigh" }.MatchesText(Incident));

    [Fact]
    public void Search_LooksAtSummaryAndType()
    {
        Assert.True(new IncidentFilter { Search = "DEGRADADO" }.MatchesText(Incident));
        Assert.True(new IncidentFilter { Search = "critical" }.MatchesText(Incident));
        Assert.False(new IncidentFilter { Search = "cpu" }.MatchesText(Incident));
    }

    [Fact]
    public void BothFiltersMustMatch()
    {
        Assert.True(new IncidentFilter { TipoContains = "nginx", Search = "5xx" }.MatchesText(Incident));
        Assert.False(new IncidentFilter { TipoContains = "host", Search = "5xx" }.MatchesText(Incident));
    }
}
