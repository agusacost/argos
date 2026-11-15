using Argos.Business.Services.Evidence;
using Argos.Entities;

namespace Argos.Tests.Evidence;

public sealed class AlertEvidenceFormatterTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddMinutes(12);
    private readonly AlertEvidenceFormatter _formatter = new();

    private static string UpstreamRefused(int i) =>
        $"2026/11/11 10:0{i % 10}:00 [error] 29#29: *{100 + i} connect() failed (111: Connection refused) while connecting " +
        $"to upstream, client: 172.18.0.{i % 200}, server: _, request: \"GET /api/usuarios/{i} HTTP/1.1\", " +
        "upstream: \"http://172.18.0.5:8080/api/usuarios\", host: \"localhost\"";

    [Fact]
    public void Empty_HasNoText()
    {
        var evidence = _formatter.Build("NginxCriticalErrorRate", Start, End, [], [], [new ContainerLogExcerpt("membresia-api", [])]);

        Assert.False(evidence.Available);
        Assert.Equal(0, evidence.LineCount);
        Assert.Equal("", evidence.Text);
        Assert.Equal("2026-11-11T10:00:00+00:00", evidence.WindowStart);
    }

    [Fact]
    public void RepeatedErrors_AreGroupedWithCount()
    {
        var lines = Enumerable.Range(0, 40).Select(UpstreamRefused)
            .Append("2026/11/11 10:05:00 [warn] 29#29: *9 an upstream response is buffered to a temporary file")
            .ToList();

        var evidence = _formatter.Build("NginxCriticalErrorRate", Start, End, [], lines, []);

        Assert.Equal(2, evidence.NginxErrors.Count);
        Assert.StartsWith("[error] connect() failed (111: Connection refused)", evidence.NginxErrors[0]);
        Assert.EndsWith("(×40)", evidence.NginxErrors[0]);
        Assert.StartsWith("[warn] an upstream response", evidence.NginxErrors[1]);
        Assert.Contains("Nginx error.log", evidence.Text);
    }

    [Fact]
    public void Budget_IsFixedAndLinesAreTruncated()
    {
        var status = Enumerable.Range(0, 10).Select(i => new NginxStatusCount($"{500 + i}", "-", 100 - i)).ToList();
        var errors = Enumerable.Range(0, 30).Select(i => $"[error] 1#1: mensaje distinto {i} " + new string('x', 400)).ToList();
        var containers = new[] { "membresia-api", "membresia-sqlserver", "nginx-proxy" }
            .Select(c => new ContainerLogExcerpt(c, Enumerable.Range(0, 20).Select(i => $"{c} línea {i}").ToList()))
            .ToList();

        var evidence = _formatter.Build("NginxCriticalErrorRate", Start, End, status, errors, containers);

        Assert.Equal(AlertEvidenceFormatter.MaxTotalLines, evidence.LineCount);
        Assert.Equal(AlertEvidenceFormatter.MaxStatusLines, evidence.StatusCounts.Count);
        Assert.Equal("500", evidence.StatusCounts[0].Status);
        Assert.Equal(AlertEvidenceFormatter.MaxContainerLines, evidence.ContainerLogs.Sum(c => c.Lines.Count));
        Assert.Equal(2, evidence.ContainerLogs.Count);
        Assert.All(evidence.NginxErrors, e => Assert.True(e.Length <= AlertEvidenceFormatter.MaxLineChars));
        // Encabezado + 3 títulos de sección + 18 líneas.
        Assert.Equal(1 + 3 + AlertEvidenceFormatter.MaxTotalLines, evidence.Text.Split('\n').Length);
    }

    [Fact]
    public void ContainerLines_PreferErrorLikeOnes()
    {
        var api = new ContainerLogExcerpt("membresia-api",
        [
            "info: Request finished HTTP/1.1 GET /api/usuarios - 500",
            "fail: Microsoft.Data.SqlClient.SqlException: A network-related error occurred while establishing a connection",
            "info: Request starting HTTP/1.1 GET /api/usuarios",
        ]);

        var evidence = _formatter.Build("NginxCriticalErrorRate", Start, End,
            [new NginxStatusCount("500", "500", 120)], [], [api]);

        var lines = Assert.Single(evidence.ContainerLogs).Lines;
        Assert.Single(lines);
        Assert.Contains("SqlException", lines[0]);
        Assert.Contains("- 500 (upstream 500): 120", evidence.Text);
        Assert.Contains("- membresia-api: fail: Microsoft.Data.SqlClient.SqlException", evidence.Text);
    }
}
