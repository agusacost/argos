using Argos.Business.Services.Rag;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Búsqueda semántica y alta de incidentes (n8n, sin auth).</summary>
public sealed class RagController(IRagService rag) : ArgosControllerBase
{
    [HttpPost("query")]
    public Task<QueryResponse> Query(QueryRequest request) => rag.QueryAsync(request);

    [HttpPost("upsert")]
    public async Task<ObjectResult> Upsert(UpsertIncidentRequest request) => Created(await rag.UpsertAsync(request));
}
