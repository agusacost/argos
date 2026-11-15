using Argos.Business.Seeding;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Repository.Knowledge;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Knowledge;

/// <summary>F-04 en instalaciones existentes: el seed completa el documento vacío sin pisar lo demás.</summary>
public sealed class SeedServiceTests
{
    private static Incident Runbook() => new()
    {
        TipoAlerta = "NginxHighErrorRate", Resumen = "r", Pasos = ["del archivo"], Documento = "## Contexto",
    };

    private static (SeedService Seed, Mock<IIncidentRepository> Repo) Create(Incident? existing)
    {
        var repo = new Mock<IIncidentRepository>();
        repo.Setup(r => r.GetExistingAsync(It.IsAny<Incident>())).ReturnsAsync(existing);
        var knowledge = new Mock<IKnowledgeSource>();
        knowledge.Setup(k => k.LoadRunbooks()).Returns([Runbook()]);
        var seed = new SeedService(repo.Object, knowledge.Object, Mock.Of<IDemoIncidentCatalog>(), Mock.Of<IClock>(),
            NullLogger<SeedService>.Instance);
        return (seed, repo);
    }

    [Fact]
    public async Task Missing_IsCreated()
    {
        var (seed, repo) = Create(existing: null);

        Assert.Equal(1, await seed.SeedKnowledgeAsync());
        repo.Verify(r => r.SaveAsync(It.Is<Incident>(i => i.Documento == "## Contexto")), Times.Once);
    }

    [Fact]
    public async Task ExistingWithoutDocument_GetsOnlyTheDocument()
    {
        var existing = new Incident { Id = "id", TipoAlerta = "NginxHighErrorRate", Resumen = "r", Pasos = ["editado a mano"] };
        var (seed, repo) = Create(existing);

        Assert.Equal(0, await seed.SeedKnowledgeAsync());
        repo.Verify(r => r.UpdateAsync(It.Is<Incident>(i =>
            i.Documento == "## Contexto" && i.Pasos.Single() == "editado a mano")), Times.Once);
        repo.Verify(r => r.SaveAsync(It.IsAny<Incident>()), Times.Never);
    }

    [Fact]
    public async Task ExistingWithDocument_IsLeftAlone()
    {
        var existing = new Incident { Id = "id", TipoAlerta = "NginxHighErrorRate", Resumen = "r", Documento = "editado" };
        var (seed, repo) = Create(existing);

        await seed.SeedKnowledgeAsync();

        repo.Verify(r => r.UpdateAsync(It.IsAny<Incident>()), Times.Never);
        repo.Verify(r => r.SaveAsync(It.IsAny<Incident>()), Times.Never);
    }
}
