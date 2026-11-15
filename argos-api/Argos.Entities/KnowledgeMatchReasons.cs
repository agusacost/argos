namespace Argos.Entities;

/// <summary>Por qué <c>POST /query</c> clasificó la alerta (campo <c>match_reason</c>).</summary>
public static class KnowledgeMatchReasons
{
    /// <summary>Un documento con conocimiento supera el umbral de similitud (criterio histórico).</summary>
    public const string Similarity = "similarity";

    /// <summary>
    /// Ninguno supera el umbral, pero hay un documento con solución del MISMO tipo de alerta por encima del piso
    /// (RAG_NAME_MATCH_MIN_SCORE).
    /// </summary>
    public const string AlertName = "alert_name";

    /// <summary>Alerta nueva.</summary>
    public const string None = "none";
}
