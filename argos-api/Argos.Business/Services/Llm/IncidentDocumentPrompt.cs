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
        "Respondé en español y devolvé SOLO el Markdown, sin texto adicional.";

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
