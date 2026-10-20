namespace Argos.Repository.Http;

/// <summary>Clientes HTTP con nombre (cada uno con su timeout).</summary>
public static class HttpClientNames
{
    public const string Qdrant = "qdrant";
    public const string Observability = "obs";
    public const string Probe = "probe";
    public const string Llm = "llm";
    public const string LlmCheck = "llm-check";
}
