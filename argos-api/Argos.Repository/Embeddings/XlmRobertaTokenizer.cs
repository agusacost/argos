using Microsoft.ML.Tokenizers;

namespace Argos.Repository.Embeddings;

/// <summary>
/// Tokenizer de XLM-R con ids estilo HuggingFace: &lt;s&gt;=0, &lt;pad&gt;=1, &lt;/s&gt;=2, &lt;unk&gt;=3 y el
/// resto desplazado +1 respecto del modelo SentencePiece (offset fairseq). Trunca a 128 tokens.
/// </summary>
public sealed class XlmRobertaTokenizer
{
    private const int MaxLength = 128;   // min(model_max_length=512, max_length=128) de tokenizer_config.json
    private const long BosId = 0, EosId = 2, UnkId = 3;

    private readonly SentencePieceTokenizer _sentencePiece;

    private XlmRobertaTokenizer(SentencePieceTokenizer sentencePiece) => _sentencePiece = sentencePiece;

    public static XlmRobertaTokenizer Load(string modelPath)
    {
        using var stream = File.OpenRead(modelPath);
        return new XlmRobertaTokenizer(SentencePieceTokenizer.Create(stream, false, false));
    }

    public long[] Encode(string text)
    {
        var spIds = _sentencePiece.EncodeToIds(text);
        var ids = new List<long>(Math.Min(spIds.Count, MaxLength - 2) + 2) { BosId };
        foreach (var id in spIds)
        {
            if (ids.Count == MaxLength - 1) break;
            ids.Add(id == 0 ? UnkId : id + 1);
        }
        ids.Add(EosId);
        return [.. ids];
    }
}
