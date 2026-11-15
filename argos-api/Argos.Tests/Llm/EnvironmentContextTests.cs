using Argos.Business.Services.Llm;
using Argos.Business.Services.Settings;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Entities;
using Argos.Repository.Qdrant;
using Argos.Repository.Repositories;
using Moq;

namespace Argos.Tests.Llm;

/// <summary>
/// El entorno monitoreado lo describe quien opera el servicio (Configuración o ARGOS_ENVIRONMENT_CONTEXT); el código de
/// Argos no conoce sus contenedores.
/// </summary>
public sealed class EnvironmentContextTests
{
    private static readonly Incident Incident = new() { TipoAlerta = "NginxCriticalErrorRate", Resumen = "5xx > 20%" };

    [Fact]
    public void CodePrompt_DoesNotNameTheMonitoredContainers()
    {
        var system = IncidentDocumentPrompt.BuildSystem(null);

        Assert.DoesNotContain("membresia", system, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nginx-proxy", system, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(IncidentDocumentPrompt.BaseSystemPrompt, system);
    }

    [Fact]
    public void ConfiguredContext_IsAppendedToTheSystemPrompt()
    {
        var prompt = new IncidentDocumentPrompt(Incident, null, "  Docker Compose; contenedores: app-api, app-db.  ");

        Assert.EndsWith(IncidentDocumentPrompt.EnvironmentHeader + "Docker Compose; contenedores: app-api, app-db.", prompt.System);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyContext_LeavesOnlyTheGenericGuidance(string context)
    {
        Assert.Equal(IncidentDocumentPrompt.BaseSystemPrompt, new IncidentDocumentPrompt(Incident, null, context).System);
    }

    [Fact]
    public void Validator_TrimsAndLimitsTheLength()
    {
        var validator = new SettingsValidator(LlmServiceFactory.WithRealProviders());

        Assert.Equal("Docker Compose", validator.NormalizeEnvironmentContext("  Docker Compose \n"));
        Assert.Throws<InvalidInputException>(() =>
            validator.NormalizeEnvironmentContext(new string('x', SettingsValidator.MaxEnvironmentContextChars + 1)));
    }

    [Theory]
    [InlineData(null, "del despliegue")]
    [InlineData("", "")]
    [InlineData("editado en el panel", "editado en el panel")]
    public async Task Repository_UsesTheDeploymentDefaultOnlyWhenNeverEdited(string? stored, string expected)
    {
        var client = new Mock<IQdrantClient>();
        client.Setup(c => c.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((QdrantPoint?)null);
        var serializer = new Mock<IPayloadSerializer>();
        var environment = new Mock<IEnvironmentContextConfig>();
        environment.SetupGet(e => e.DefaultEnvironmentContext).Returns("del despliegue");
        var repository = new SettingsRepository(client.Object, serializer.Object, environment.Object);
        if (stored is not null)
        {
            client.Setup(c => c.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QdrantPoint("1", new System.Text.Json.Nodes.JsonObject()));
            serializer.Setup(s => s.Deserialize<AppSettings>(It.IsAny<System.Text.Json.Nodes.JsonObject>()))
                .Returns(new AppSettings { EnvironmentContext = stored });
        }

        var settings = await repository.GetAsync();

        Assert.Equal(expected, settings.EnvironmentContext);
    }
}
