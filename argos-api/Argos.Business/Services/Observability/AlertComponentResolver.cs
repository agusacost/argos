namespace Argos.Business.Services.Observability;

public sealed class AlertComponentResolver : IAlertComponentResolver
{
    private static readonly Dictionary<string, string> LokiServices = new()
    {
        ["nginx"] = "nginx", ["api"] = "api", ["postgresql"] = "postgresql", ["redis"] = "redis", ["host"] = "host",
    };

    private static readonly Dictionary<string, string> PrometheusExpressions = new()
    {
        ["nginx"] = "rate(nginx_http_requests_total[2m])",
        ["api"] = "rate(http_requests_total[2m])",
        ["postgresql"] = "pg_stat_activity_count",
        ["redis"] = "redis_connected_clients",
        ["host"] = "node_load1",
        ["kubernetes"] = "kube_pod_status_ready",
    };

    public string ComponentOf(string tipoAlerta)
    {
        var t = tipoAlerta.ToLowerInvariant();
        if (t.Contains("nginx")) return "nginx";
        if (t.Contains("postgresql") || t.Contains("postgres") || t.Contains("sql")) return "postgresql";
        if (t.Contains("redis")) return "redis";
        if (t.Contains("api") || t.Contains("gateway")) return "api";
        if (t.Contains("kubernetes") || t.Contains("k8s") || t.Contains("pod")) return "kubernetes";
        if (t.Contains("loki")) return "loki";
        if (t.Contains("host") || t.Contains("cpu") || t.Contains("memory") || t.Contains("disk")) return "host";
        return "unknown";
    }

    public string LokiServiceOf(string component) => LokiServices.GetValueOrDefault(component, component);

    public string? PrometheusExpressionOf(string component) => PrometheusExpressions.GetValueOrDefault(component);
}
