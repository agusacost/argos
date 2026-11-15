namespace Argos.Entities;

/// <summary>Requests del access.log de Nginx con un par status / upstream_status en la ventana ("-" = sin upstream).</summary>
public sealed record NginxStatusCount(string Status, string UpstreamStatus, long Count);
