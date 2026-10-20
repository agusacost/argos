namespace Argos.Entities;

/// <summary>Valores del campo <c>estado</c> de un incidente.</summary>
public static class IncidentStates
{
    public const string Nuevo = "nuevo";
    public const string Pendiente = "pendiente";
    public const string Resuelto = "resuelto";
    public const string DocumentadoPreviamente = "documentado_previamente";
    public const string AutoDocumentado = "auto_documentado";

    /// <summary>Estados que forman la base de conocimiento (runbooks manuales y docs generados por el LLM).</summary>
    public static readonly IReadOnlyList<string> Knowledge = [DocumentadoPreviamente, AutoDocumentado];
}
