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
}
