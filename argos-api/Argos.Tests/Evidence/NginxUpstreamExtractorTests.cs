using Argos.Business.Services.Evidence;

namespace Argos.Tests.Evidence;

/// <summary>Líneas reales del error.log de Nginx registradas durante las fallas F1 y F2 (11/11/2026).</summary>
public sealed class NginxUpstreamExtractorTests
{
    private readonly NginxUpstreamExtractor _extractor = new();

    [Fact]
    public void UpstreamAddr_FromTheAccessLog_WithoutPort()
    {
        var hosts = _extractor.Extract([("172.20.0.23:8080", 221)], []);

        Assert.Equal(["172.20.0.23"], hosts);
    }

    [Fact]
    public void RetriedUpstreams_AreSplitAndCounted()
    {
        var hosts = _extractor.Extract([("172.20.0.23:8080, 172.20.0.24:8080", 10), ("172.20.0.24:8080", 5)], []);

        Assert.Equal(["172.20.0.24", "172.20.0.23"], hosts);
    }

    [Fact]
    public void ConnectFailed_TakesTheUpstreamOfTheErrorLog()
    {
        const string line = "2026/11/11 22:27:59 [error] 19#19: *1657 connect() failed (111: Connection refused) while " +
                            "connecting to upstream, client: 172.20.0.12, server: api.observabilidad.local, request: " +
                            "\"GET /api/status HTTP/1.1\", upstream: \"http://172.20.0.12:8080/api/status\", host: \"nginx-proxy\"";

        Assert.Equal(["172.20.0.12"], _extractor.Extract([], [line]));
    }

    [Fact]
    public void CouldNotBeResolved_TakesTheHostName()
    {
        // Con resolver por request, un contenedor detenido desaparece del DNS de Docker.
        const string line = "2026/11/11 22:44:56 [error] 20#20: *13 membresia-api could not be resolved (2: Server failure), " +
                            "client: 172.20.0.12, server: api.observabilidad.local, request: \"GET /api/usuarios HTTP/1.1\"";

        Assert.Equal(["membresia-api"], _extractor.Extract([], [line]));
    }

    [Fact]
    public void NothingReported_NoHosts()
    {
        Assert.Empty(_extractor.Extract([("-", 3)], ["2026/11/11 22:00:00 [warn] 1#1: something unrelated"]));
    }

    [Fact]
    public void AtMostTwoUpstreams()
    {
        var hosts = _extractor.Extract([("10.0.0.1:80", 3), ("10.0.0.2:80", 2), ("10.0.0.3:80", 1)], []);

        Assert.Equal(NginxUpstreamExtractor.MaxUpstreams, hosts.Count);
    }
}
