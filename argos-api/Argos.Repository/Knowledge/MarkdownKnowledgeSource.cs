using System.Text;
using Argos.Common.Configuration;
using Argos.Entities;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Argos.Repository.Knowledge;

/// <summary>
/// Runbooks de <c>knowledge/*.md</c> con front-matter YAML (tipo_alerta, resumen, pasos, estado, fuente). El cuerpo
/// Markdown después del front-matter (Contexto, Causas, …) va al campo <c>documento</c> (F-04: antes se descartaba y el
/// panel no mostraba la documentación de los runbooks manuales).
/// </summary>
public sealed class MarkdownKnowledgeSource(ISeedConfig config, ILogger<MarkdownKnowledgeSource> log) : IKnowledgeSource
{
    private const string FrontMatterDelimiter = "---";

    public IReadOnlyList<Incident> LoadRunbooks()
    {
        var dir = config.KnowledgeDir;
        if (!Directory.Exists(dir))
        {
            log.LogInformation("[seed] knowledge/ no encontrada, se omite.");
            return [];
        }
        var yaml = new DeserializerBuilder().Build();
        var runbooks = new List<Incident>();
        foreach (var path in Directory.GetFiles(dir, "*.md").OrderBy(p => p, StringComparer.Ordinal))
        {
            try
            {
                if (SplitFrontMatter(File.ReadAllText(path, Encoding.UTF8)) is not var (frontMatter, body)) continue;
                var incident = ToIncident(yaml.Deserialize<Dictionary<string, object?>>(frontMatter));
                incident.Documento = body.Length > 0 ? body : null;
                runbooks.Add(incident);
            }
            catch (Exception ex)
            {
                log.LogWarning("[seed] Error en {File}: {Error}", Path.GetFileName(path), ex.Message);
            }
        }
        return runbooks;
    }

    /// <summary>(front-matter YAML, cuerpo Markdown sin espacios sobrantes), o null si el archivo no empieza con '---'.</summary>
    public static (string FrontMatter, string Body)? SplitFrontMatter(string text)
    {
        text = text.TrimStart('﻿');
        if (!text.StartsWith(FrontMatterDelimiter, StringComparison.Ordinal)) return null;
        var parts = text.Split(FrontMatterDelimiter, 3);
        if (parts.Length < 3) return null;
        return (parts[1], parts[2].Trim());
    }

    private static Incident ToIncident(Dictionary<string, object?> data) => new()
    {
        TipoAlerta = (string)data["tipo_alerta"]!,
        Resumen = (string)data["resumen"]!,
        Pasos = data.TryGetValue("pasos", out var p) && p is List<object> l ? l.Select(x => x?.ToString() ?? "").ToList() : [],
        Estado = data.GetValueOrDefault("estado") as string ?? IncidentStates.DocumentadoPreviamente,
        Fuente = data.GetValueOrDefault("fuente") as string ?? "manual",
    };
}
