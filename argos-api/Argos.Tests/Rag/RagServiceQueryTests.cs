using Argos.Business.Services.Notifications;
using Argos.Business.Services.Rag;
using Argos.Business.Services.Settings;
using Argos.Common.Configuration;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Rag;

/// <summary><c>POST /query</c> con el nombre de la alerta: el documento del mismo tipo se busca aparte y se devuelve.</summary>
public sealed class RagServiceQueryTests
{
    private static ScoredIncident Doc(string tipo, double score) => new(new Incident
    {
        Id = Guid.NewGuid().ToString(), TipoAlerta = tipo, Resumen = tipo, Pasos = ["p"],
        Estado = IncidentStates.DocumentadoPreviamente,
    }, score);

    private static RagService Create(Mock<IIncidentRepository> repo)
    {
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.GetSimilarityThresholdAsync()).ReturnsAsync(0.68);
        var config = new Mock<IQdrantConfig>();
        config.SetupGet(c => c.NameMatchMinScore).Returns(0.50);
        var matcher = new KnowledgeMatcher(repo.Object, settings.Object, new KnowledgeClassifier(), config.Object);
        return new RagService(repo.Object, matcher, Mock.Of<INotificationService>(), Mock.Of<IClock>(),
            NullLogger<RagService>.Instance);
    }

    [Fact]
    public async Task SameAlertOutsideTopK_IsAppendedAndMakesItKnown()
    {
        var own = Doc("NginxSuspiciousIPActivity", 0.607);
        var repo = new Mock<IIncidentRepository>();
        repo.Setup(r => r.SearchSimilarAsync("texto", 1, null)).ReturnsAsync([Doc("NginxHighErrorRate", 0.64)]);
        repo.Setup(r => r.SearchSimilarAsync("texto", It.IsAny<int>(), "NginxSuspiciousIPActivity")).ReturnsAsync([own]);

        var response = await Create(repo).QueryAsync(new QueryRequest
        {
            Text = "texto", TopK = 1, Alertname = "NginxSuspiciousIPActivity",
        });

        Assert.True(response.IsDocumented);
        Assert.Equal(KnowledgeMatchReasons.AlertName, response.MatchReason);
        Assert.Equal(2, response.Results.Count);
        Assert.Equal(IncidentMatch.Nuevo, response.Results[0].Clasificacion);
        Assert.Equal(IncidentMatch.Documentado, response.Results[1].Clasificacion);
        Assert.Equal("NginxSuspiciousIPActivity", response.Results[1].TipoAlerta);
    }

    [Fact]
    public async Task WithoutAlertname_DoesNotSearchByType()
    {
        var repo = new Mock<IIncidentRepository>();
        repo.Setup(r => r.SearchSimilarAsync("texto", 3, null)).ReturnsAsync([Doc("NginxHighResponseTime", 0.669)]);

        var response = await Create(repo).QueryAsync(new QueryRequest { Text = "texto" });

        Assert.False(response.IsDocumented);
        Assert.Equal(KnowledgeMatchReasons.None, response.MatchReason);
        repo.Verify(r => r.SearchSimilarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsNotNull<string>()), Times.Never);
    }
}
