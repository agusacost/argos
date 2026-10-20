namespace Argos.Repository.Embeddings;

/// <summary>Vectoriza texto para la búsqueda semántica de incidentes.</summary>
public interface ITextEmbedder
{
    /// <summary>Mismo vector para documentos y consultas (el modelo no usa prefijos).</summary>
    float[] Embed(string text);
}
