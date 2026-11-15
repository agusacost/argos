using Argos.Business.Services.Privacy;
using Argos.Entities;
using Argos.Repository.Llm;
using Moq;

namespace Argos.Tests.Llm;

/// <summary>El camino de generación es el único punto por donde sale texto al proveedor: ahí no debe ir ninguna IP.</summary>
public sealed class LlmServicePseudonymizationTests
{
    private const string User = "La IP 172.18.0.25 generó 1840 req/min.\nclient: 172.18.0.25, upstream: \"http://10.0.0.5:8080\"";

    private static (Mock<ILlmProvider> Provider, List<(string System, string User)> Sent) FakeProvider(string answer)
    {
        var sent = new List<(string, string)>();
        var provider = new Mock<ILlmProvider>();
        provider.SetupGet(p => p.Name).Returns("gemini");
        provider.SetupGet(p => p.DefaultModel).Returns("gemini-flash-lite-latest");
        provider.SetupGet(p => p.IsConfigured).Returns(true);
        provider.Setup(p => p.AcceptsModel(It.IsAny<string>())).Returns(true);
        provider.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((s, u, _) => sent.Add((s, u)))
            .ReturnsAsync(answer);
        return (provider, sent);
    }

    [Fact]
    public async Task ChainGeneration_SendsNoIpAndRestoresTheAnswer()
    {
        var (provider, sent) = FakeProvider("Bloquear [IP-1] con deny; revisar el upstream [IP-2].");
        var llm = LlmServiceFactory.Create([provider.Object], pseudonymizeIps: true);

        var result = await llm.GenerateAsync("Sos un analista.", User, new AppSettings { LlmProvider = "gemini" });

        var (system, user) = Assert.Single(sent);
        Assert.DoesNotContain("172.18.0.25", user);
        Assert.DoesNotContain("10.0.0.5", user);
        Assert.Contains("[IP-1]", user);
        Assert.EndsWith(IpPseudonymizationSession.ModelNotice, system);
        Assert.Equal("Bloquear 172.18.0.25 con deny; revisar el upstream 10.0.0.5.", result.Text);
        Assert.Equal(2, result.PseudonymizedIps);
    }

    [Fact]
    public async Task ForcedProvider_IsAlsoPseudonymized()
    {
        var (provider, sent) = FakeProvider("ok [IP-1]");
        var llm = LlmServiceFactory.Create([provider.Object], pseudonymizeIps: true);

        var result = await llm.GenerateWithAsync("gemini", null, "s", User);

        Assert.DoesNotContain("172.18.0.25", Assert.Single(sent).User);
        Assert.Equal("ok 172.18.0.25", result.Text);
    }

    [Fact]
    public async Task Disabled_SendsTheTextAsIs()
    {
        var (provider, sent) = FakeProvider("respuesta");
        var llm = LlmServiceFactory.Create([provider.Object], pseudonymizeIps: false);

        var result = await llm.GenerateAsync("s", User, new AppSettings { LlmProvider = "gemini" });

        Assert.Equal(User, Assert.Single(sent).User);
        Assert.Equal(0, result.PseudonymizedIps);
    }
}
