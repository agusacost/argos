using Argos.Entities.Dtos;

namespace Argos.Business.Services.Rag;

/// <summary>Búsqueda semántica y alta de incidentes en la base de conocimiento (n8n).</summary>
public interface IRagService
{
    /// <summary>Incidentes parecidos y si la alerta ya está documentada según el umbral vigente.</summary>
    Task<QueryResponse> QueryAsync(QueryRequest request);

    /// <summary>Indexa el incidente (mismo tipo + resumen = mismo punto).</summary>
    Task<UpsertResponse> UpsertAsync(UpsertIncidentRequest request);
}
