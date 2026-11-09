using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Llm;

/// <summary>Generación para n8n: cadena de fallback y, si fallan todos los proveedores, aviso en la campana.</summary>
public interface IPipelineLlmService
{
    Task<LlmGeneration> GenerateAsync(LlmGenerateRequest request);
}
