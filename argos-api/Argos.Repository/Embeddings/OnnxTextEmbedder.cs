using Argos.Common.Configuration;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Argos.Repository.Embeddings;

/// <summary>
/// Embeddings locales con <c>paraphrase-multilingual-MiniLM-L12-v2</c>
/// (ONNX cuantizado de <c>qdrant/paraphrase-multilingual-MiniLM-L12-v2-onnx-Q</c>):
/// tokenizer XLM-R → last_hidden_state → mean pooling (384 dims). Sin normalizar: Qdrant (Cosine)
/// normaliza al indexar y buscar.
/// El modelo se carga en el primer uso.
/// </summary>
public sealed class OnnxTextEmbedder : ITextEmbedder, IDisposable
{
    private readonly Lazy<XlmRobertaTokenizer> _tokenizer;
    private readonly Lazy<InferenceSession> _session;
    private readonly int _dimensions;

    public OnnxTextEmbedder(IQdrantConfig config)
    {
        var dir = config.EmbeddingModelDir;
        _dimensions = config.VectorSize;
        _tokenizer = new Lazy<XlmRobertaTokenizer>(() => XlmRobertaTokenizer.Load(Path.Combine(dir, "sentencepiece.bpe.model")));
        _session = new Lazy<InferenceSession>(() => new InferenceSession(Path.Combine(dir, "model_optimized.onnx")));
    }

    public float[] Embed(string text)
    {
        var ids = _tokenizer.Value.Encode(text);
        int n = ids.Length;

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(ids, [1, n])),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(Enumerable.Repeat(1L, n).ToArray(), [1, n])),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(new long[n], [1, n])),
        };
        using var results = _session.Value.Run(inputs);
        var hidden = results.First().AsTensor<float>();

        // Mean pooling sobre la máscara de atención (sin padding: un texto por llamada).
        var vec = new float[_dimensions];
        for (int t = 0; t < n; t++)
            for (int d = 0; d < _dimensions; d++)
                vec[d] += hidden[0, t, d];
        for (int d = 0; d < _dimensions; d++) vec[d] /= n;
        return vec;
    }

    public void Dispose()
    {
        if (_session.IsValueCreated) _session.Value.Dispose();
    }
}
