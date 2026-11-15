using Argos.Business.Services.Notifications;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Rag;

public sealed class RagService(
    IIncidentRepository incidents,
    IKnowledgeMatcher matcher,
    INotificationService notifications,
    IClock clock,
    ILogger<RagService> log) : IRagService
{
    // Fuentes con las que n8n registra los incidentes (pendiente o auto-documentado).
    private static readonly string[] PipelineSources = ["alertmanager", "auto-generado"];

    public async Task<QueryResponse> QueryAsync(QueryRequest request)
    {
        var text = request.Text!;
        var topK = request.TopK ?? QueryRequest.DefaultTopK;
        var lookup = await matcher.LookupAsync(text, request.Alertname, topK);
        var (threshold, classification) = (lookup.Threshold, lookup.Classification);
        var byName = classification.Reason == KnowledgeMatchReasons.AlertName ? classification.Best : null;

        // Los top-k de siempre; si la alerta es conocida por nombre y su documento no está entre ellos, se agrega al
        // final (orden por score) para que el LLM lo reciba como contexto.
        var shown = lookup.Candidates.Take(topK).ToList();
        if (byName is not null && !shown.Contains(byName)) shown.Add(byName);
        var matches = shown
            .Select(h => new IncidentMatch(h.Incident, h.Score, threshold, knownByAlertName: ReferenceEquals(h, byName)))
            .ToList();
        return new QueryResponse(text, threshold, classification.IsKnown, matches, classification.Reason,
            lookup.NameMatchMinScore);
    }

    public async Task<UpsertResponse> UpsertAsync(UpsertIncidentRequest request)
    {
        await incidents.EnsureCollectionAsync();
        var incident = await incidents.SaveAsync(new Incident
        {
            TipoAlerta = request.TipoAlerta!,
            Resumen = request.Resumen!,
            Pasos = request.Pasos ?? [],
            Fecha = request.Fecha ?? clock.UtcNowNaive(),
            Estado = request.Estado ?? IncidentStates.DocumentadoPreviamente,
            Fuente = request.Fuente ?? "manual",
            Documento = request.Documento,
            Severity = request.Severity,
            DetectedAt = request.DetectedAt,
            ResolvedAt = request.ResolvedAt,
            MttrMinutes = request.MttrMinutes,
        });
        if (PipelineSources.Contains(incident.Fuente)) await NotifyArrivalAsync(incident);
        return new UpsertResponse(incident.Id!, incident.TipoAlerta);
    }

    // El incidente ya quedó guardado: un aviso que falla no debe hacer fallar el alta.
    private async Task NotifyArrivalAsync(Incident incident)
    {
        try
        {
            await notifications.NotifyIncidentArrivalAsync(incident);
        }
        catch (Exception ex)
        {
            log.LogWarning("[upsert] no se pudo guardar la notificación de {Alert}: {Error}", incident.TipoAlerta, ex.Message);
        }
    }
}
