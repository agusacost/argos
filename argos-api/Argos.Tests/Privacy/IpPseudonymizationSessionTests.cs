using Argos.Business.Services.Privacy;

namespace Argos.Tests.Privacy;

public sealed class IpPseudonymizationSessionTests
{
    [Fact]
    public void Mask_ReplacesIpv4WithStableMarkers()
    {
        var session = new IpPseudonymizationSession(enabled: true);

        var masked = session.Mask("client: 172.18.0.25, upstream: \"http://172.18.0.5:8080/api\", otra vez 172.18.0.25");

        Assert.Equal("client: [IP-1], upstream: \"http://[IP-2]:8080/api\", otra vez [IP-1]", masked);
        Assert.Equal(2, session.Count);
    }

    [Fact]
    public void Restore_PutsBackTheRealAddresses()
    {
        var session = new IpPseudonymizationSession(enabled: true);
        var original = "La IP 172.18.0.25 generó 1840 req/min; bloquear 172.18.0.25 y revisar 10.0.0.1.";

        var masked = session.Mask(original);

        Assert.DoesNotContain("172.18", masked);
        Assert.Equal(original, session.Restore(masked));
        Assert.Equal("deny 172.18.0.25;", session.Restore("deny [IP-1];"));
    }

    [Fact]
    public void Restore_IgnoresMarkersThatAreNotFromTheSession()
    {
        var session = new IpPseudonymizationSession(enabled: true);
        session.Mask("10.1.1.1");

        Assert.Equal("10.1.1.1 y [IP-7]", session.Restore("[IP-1] y [IP-7]"));
        Assert.Equal("10.1.1.1", session.Restore("[ip-1]"));
    }

    [Theory]
    [InlineData("nginx/1.25.3")]
    [InlineData("v1.2.3.4")]
    [InlineData("1.2.3.4.5")]
    [InlineData("256.1.1.1")]
    [InlineData("El 87.5% de los requests")]
    [InlineData("2026-11-11T10:20:30Z")]
    [InlineData("11/Nov/2026:10:20:30 +0000")]
    [InlineData("MAC aa:bb:cc:dd:ee:ff")]
    [InlineData("std::string y a::b::c")]
    public void Mask_LeavesNonAddressesUntouched(string text)
    {
        var session = new IpPseudonymizationSession(enabled: true);

        Assert.Equal(text, session.Mask(text));
        Assert.Equal(0, session.Count);
    }

    [Theory]
    [InlineData("desde 2001:db8::8a2e:370:7334 hubo", "desde [IP-1] hubo")]
    [InlineData("loopback ::1 ok", "loopback [IP-1] ok")]
    [InlineData("[fe80::1]:8080", "[[IP-1]]:8080")]
    [InlineData("mapeada ::ffff:192.168.0.1 fin", "mapeada [IP-1] fin")]
    [InlineData("addr fe80::1: error", "addr [IP-1]: error")]
    [InlineData("1:2:3:4:5:6:7:8", "[IP-1]")]
    public void Mask_ReplacesIpv6(string text, string expected)
    {
        var session = new IpPseudonymizationSession(enabled: true);

        var masked = session.Mask(text);

        Assert.Equal(expected, masked);
        Assert.Equal(text, session.Restore(masked));
    }

    [Fact]
    public void Disabled_DoesNotChangeTheText()
    {
        var session = new IpPseudonymizationSession(enabled: false);
        var (system, user) = session.MaskPrompts("sistema", "IP 10.0.0.1");

        Assert.Equal("sistema", system);
        Assert.Equal("IP 10.0.0.1", user);
        Assert.Equal(0, session.Count);
    }

    [Fact]
    public void MaskPrompts_SharesMarkersAndAddsNoticeOnlyWhenNeeded()
    {
        var session = new IpPseudonymizationSession(enabled: true);
        var (system, user) = session.MaskPrompts("Analizá 10.0.0.9", "La IP 10.0.0.9 y 10.0.0.10");

        Assert.Equal("La IP [IP-1] y [IP-2]", user);
        Assert.StartsWith("Analizá [IP-1]", system);
        Assert.EndsWith(IpPseudonymizationSession.ModelNotice, system);

        var clean = new IpPseudonymizationSession(enabled: true).MaskPrompts("sistema", "sin direcciones");
        Assert.Equal("sistema", clean.System);
    }
}
