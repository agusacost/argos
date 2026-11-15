namespace Argos.Common.Configuration;

/// <summary>Conexión a Qdrant y parámetros del RAG.</summary>
public interface IQdrantConfig
{
    string QdrantUrl { get; }
    string CollectionName { get; }
    int VectorSize { get; }
    string Distance { get; }
    double SimilarityThreshold { get; }
    string EmbeddingModelDir { get; }

    /// <summary>
    /// RAG_NAME_MATCH_MIN_SCORE (default 0.30): similitud mínima para que un documento con el MISMO tipo de alerta
    /// cuente como conocido aunque no supere el umbral. Con 1 o más la señal por nombre queda desactivada.
    /// </summary>
    double NameMatchMinScore { get; }
}
