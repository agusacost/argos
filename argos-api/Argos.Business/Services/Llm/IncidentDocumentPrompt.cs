using Argos.Entities;

namespace Argos.Business.Services.Llm;

/// <summary>
/// Prompt de documentación de un incidente nuevo. Replica los nodos n8n 'Build Doc Prompt' +
/// 'Doc LLM Call' para que el documento salga igual desde el dashboard y desde n8n.
/// </summary>
public sealed class IncidentDocumentPrompt
{
    public const string SystemPrompt =
        "Sos un ingeniero SRE documentando un incidente NUEVO para la base de conocimiento de Argos. " +
        "Generá documentación técnica concisa en Markdown con estas secciones exactas: " +
        "\"# Título\", \"## Descripción\", \"## Causa raíz probable\", " +
        "\"## Pasos de resolución\" (lista numerada), \"## Prevención\". " +
        "Respondé en español y devolvé SOLO el Markdown, sin texto adicional. " + StackContext;

    /// <summary>
    /// Entorno real del stack (mismo texto que en los nodos n8n 'Build Prompt' / 'Build Doc Prompt'): sin esto los modelos
    /// proponen comandos genéricos (systemd, kubectl) que no aplican (evaluación OE3, 2026-10-27).
    /// </summary>
    public const string StackContext =
        "Entorno de Argos: un único host con Docker Compose (proyecto argos-platform), sin Kubernetes ni systemd. " +
        "Contenedores: nginx-proxy (reverse proxy, puerto 80), membresia-api (API .NET, upstream de Nginx; en el host :5000), " +
        "membresia-sqlserver (SQL Server de la API), nginx-prometheus-exporter (:9113), node-exporter (:9100), cadvisor (:8080), " +
        "alloy (envía los logs de Nginx a Loki), argos-platform-prometheus-1, argos-platform-loki-1, argos-platform-alertmanager-1, " +
        "argos-platform-grafana-1. En los comandos usá esos nombres reales (docker ps -a, docker logs, docker start/restart).";

    private const int AlertsCount = 1;

    public IncidentDocumentPrompt(Incident incident)
    {
        var alertname = string.IsNullOrEmpty(incident.TipoAlerta) ? "Incidente" : incident.TipoAlerta;
        var severity = incident.Severity ?? "warning";
        UserPrompt =
            $"Alerta: {alertname} (severidad: {severity})\n" +
            $"Resumen: {incident.Resumen}\n" +
            $"Descripción: {incident.Resumen}\n" +
            $"Alertas activas en el grupo: {AlertsCount}\n\n" +
            "Este incidente NO existe en la base de conocimiento de Argos. " +
            "Generá su documentación técnica completa.";
    }

    public string System => SystemPrompt;

    public string UserPrompt { get; }
}
