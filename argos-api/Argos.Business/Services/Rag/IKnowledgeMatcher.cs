using Argos.Entities;

namespace Argos.Business.Services.Rag;

/// <summary>
/// Busca una alerta en la base de conocimiento y la clasifica (umbral + coincidencia de nombre). Lo usan
/// <c>POST /query</c> y el registro de ocurrencias conocidas, así los dos deciden igual.
/// </summary>
public interface IKnowledgeMatcher
{
    Task<KnowledgeLookup> LookupAsync(string text, string? alertname, int topK);
}
