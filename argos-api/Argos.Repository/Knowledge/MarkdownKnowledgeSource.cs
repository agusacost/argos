using System.Text;
using Argos.Common.Configuration;
using Argos.Entities;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Argos.Repository.Knowledge;

/// <summary>Runbooks de <c>knowledge/*.md</c> con front-matter YAML (tipo_alerta, resumen, pasos, estado, fuente).</summary>
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
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (!text.StartsWith(FrontMatterDelimiter, StringComparison.Ordinal)) continue;
                var data = yaml.Deserialize<Dictionary<string, object?>>(text.Split(FrontMatterDelimiter, 3)[1]);
                runbooks.Add(ToIncident(data));
            }
            catch (Exception ex)
            {
                log.LogWarning("[seed] Error en {File}: {Error}", Path.GetFileName(path), ex.Message);
            }
        }
        return runbooks;
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
