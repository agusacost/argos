using Argos.Entities;

namespace Argos.Business.Services.Llm;

/// <summary>
/// Prompt de documentación de un incidente nuevo. Replica los nodos n8n 'Build Doc Prompt' +
/// 'Doc LLM Call' para que el documento salga igual desde el dashboard y desde n8n.
/// </summary>
public sealed class IncidentDocumentPrompt
{
    public const string BaseSystemPrompt =
        "Sos un ingeniero SRE documentando un incidente NUEVO para la base de conocimiento de Argos. " +
        "Generá documentación técnica concisa en Markdown con estas secciones exactas: " +
        "\"# Título\", \"## Descripción\", \"## Causa raíz probable\", " +
        "\"## Pasos de resolución\" (lista numerada), \"## Prevención\". " +
        "Respondé en español y devolvé SOLO el Markdown, sin texto adicional. " + EvidenceGuidance + " " + EnvironmentGuidance;

    /// <summary>
    /// Cómo usar la evidencia de logs (mismo texto que en los nodos n8n 'Build Prompt' / 'Build Doc Prompt'): sin
    /// logs, F1 (API caída) y F2 (SQL caído) recibían la misma propuesta porque disparan la misma alerta.
    /// </summary>
    public const string EvidenceGuidance =
        "Si se incluye evidencia de logs, usala para discriminar la causa raíz (por ejemplo, si el upstream no acepta " +
        "conexiones o si responde con error) y no afirmes nada que la evidencia no muestre; si no alcanza, decí qué verificar.";

    /// <summary>
    /// Instrucción genérica sobre el entorno (mismo texto que en n8n). Argos no conoce el servicio monitoreado: los
    /// nombres reales llegan por el contexto del entorno (Configuración), la evidencia o los incidentes históricos. Sin
    /// ese contexto los modelos proponían comandos de otras plataformas (systemd, kubectl) (evaluación OE3, 2026-10-27).
    /// </summary>
    public const string EnvironmentGuidance =
        "Proponé comandos que correspondan al entorno descripto en el contexto del entorno y usá los nombres de servicios y " +
        "contenedores que aparezcan ahí, en la evidencia o en los incidentes históricos; no inventes nombres ni uses " +
        "herramientas de otras plataformas que el contexto no mencione.";

    /// <summary>Encabezado con el que el contexto del entorno entra al system prompt (mismo texto que en n8n).</summary>
    public const string EnvironmentHeader = "Contexto del entorno (lo describe quien opera el servicio): ";

    /// <summary>System prompt con el contexto del entorno vigente (vacío = solo la instrucción genérica).</summary>
    public static string BuildSystem(string? environmentContext) =>
        string.IsNullOrWhiteSpace(environmentContext)
            ? BaseSystemPrompt
            : BaseSystemPrompt + " " + EnvironmentHeader + environmentContext.Trim();

    private const int AlertsCount = 1;

    /// <param name="evidence">Sección de evidencia de logs ya formateada (AlertEvidence.Text), o null.</param>
    /// <param name="environmentContext">Contexto del entorno de Configuración (AppSettings.EnvironmentContext).</param>
    public IncidentDocumentPrompt(Incident incident, string? evidence = null, string? environmentContext = null)
    {
        System = BuildSystem(environmentContext);
        var alertname = string.IsNullOrEmpty(incident.TipoAlerta) ? "Incidente" : incident.TipoAlerta;
        var severity = incident.Severity ?? "warning";
        UserPrompt =
            $"Alerta: {alertname} (severidad: {severity})\n" +
            $"Resumen: {incident.Resumen}\n" +
            $"Descripción: {incident.Resumen}\n" +
            $"Alertas activas en el grupo: {AlertsCount}\n\n" +
            (string.IsNullOrWhiteSpace(evidence) ? "" : evidence.Trim() + "\n\n") +
            "Este incidente NO existe en la base de conocimiento de Argos. " +
            "Generá su documentación técnica completa.";
    }

    public string System { get; }

    public string UserPrompt { get; }
}
