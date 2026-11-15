using Argos.Common.Configuration;
using Argos.Entities;
using Argos.Repository.Knowledge;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Argos.Tests.Knowledge;

/// <summary>F-04: el cuerpo Markdown de los runbooks se guarda en <c>documento</c>.</summary>
public sealed class MarkdownKnowledgeSourceTests
{
    [Fact]
    public void SplitFrontMatter_SeparatesYamlAndBody()
    {
        var split = MarkdownKnowledgeSource.SplitFrontMatter("﻿---\ntipo_alerta: X\n---\n\n## Contexto\n\nTexto --- con guiones\n");

        Assert.NotNull(split);
        Assert.Equal("\ntipo_alerta: X\n", split.Value.FrontMatter);
        Assert.Equal("## Contexto\n\nTexto --- con guiones", split.Value.Body);
    }

    [Fact]
    public void SplitFrontMatter_WithoutFrontMatter_IsNull() =>
        Assert.Null(MarkdownKnowledgeSource.SplitFrontMatter("# Solo markdown"));

    [Fact]
    public void DeploymentRunbooks_AllHaveDocument()
    {
        var config = new Mock<ISeedConfig>();
        config.SetupGet(c => c.KnowledgeDir).Returns(FindKnowledgeDir());

        var runbooks = new MarkdownKnowledgeSource(config.Object, NullLogger<MarkdownKnowledgeSource>.Instance).LoadRunbooks();

        Assert.Equal(5, runbooks.Count);
        Assert.All(runbooks, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Documento), $"{r.TipoAlerta} sin documento");
            Assert.StartsWith("## ", r.Documento);
            Assert.NotEmpty(r.Pasos);
            Assert.Equal(IncidentStates.DocumentadoPreviamente, r.Estado);
        });
    }

    private static string FindKnowledgeDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "docker", "monitoring", "knowledge");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("No se encontró docker/monitoring/knowledge");
    }
}
