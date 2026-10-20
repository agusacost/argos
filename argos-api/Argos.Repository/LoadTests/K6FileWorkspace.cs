using System.Text;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Repository.LoadTests;

public sealed class K6FileWorkspace(ILoadTestConfig config) : IK6Workspace
{
    public string? ReadScript(string fileName)
    {
        try { return File.ReadAllText(Path.Combine(config.K6TestsDir, fileName), Encoding.UTF8); }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    public void PrepareScript(string fileName)
    {
        try
        {
            Directory.CreateDirectory(config.K6ScriptsDir);
            File.Copy(Path.Combine(config.K6TestsDir, fileName), Path.Combine(config.K6ScriptsDir, fileName), overwrite: true);
            // Los scripts leen /data/... (mismo volumen montado dos veces).
            if (Directory.Exists(config.K6DataDir))
                foreach (var f in Directory.GetFiles(config.K6DataDir))
                    File.Copy(f, Path.Combine(config.K6ScriptsDir, Path.GetFileName(f)), overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ExternalServiceException(ex.Message, ex);
        }
    }

    public LoadTestSummary? ReadSummary(string runId)
    {
        JsonNode? raw;
        try { raw = JsonNode.Parse(File.ReadAllText(Path.Combine(config.K6ResultsDir, $"{runId}.json"), Encoding.UTF8)); }
        catch (Exception) { return null; }
        var metrics = raw?["metrics"] as JsonObject ?? [];
        return new LoadTestSummary
        {
            Reqs = Value(metrics, "http_reqs", "count"),
            ThroughputRps = Value(metrics, "http_reqs", "rate"),
            P95Ms = Value(metrics, "http_req_duration", "p(95)"),
            AvgMs = Value(metrics, "http_req_duration", "avg"),
            // Métricas Rate: la fracción (0..1) va en "value", no en "rate".
            ErrorRate = Value(metrics, "http_req_failed", "value"),
            ChecksRate = Value(metrics, "checks", "value"),
        };
    }

    private static double? Value(JsonObject metrics, string metric, string key)
    {
        var m = metrics[metric] as JsonObject ?? [];
        // El summary-export puede anidar en "values".
        var values = m.ContainsKey("values") ? m["values"] as JsonObject : m;
        return values?[key] is JsonValue v && v.TryGetValue<double>(out var d) ? RoundingMath.Round(d, 2) : null;
    }
}
