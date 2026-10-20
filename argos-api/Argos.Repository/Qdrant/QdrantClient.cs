using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Repository.Http;

namespace Argos.Repository.Qdrant;

/// <summary>
/// Cliente REST de Qdrant (puerto 6333). Trabaja con JSON crudo para preservar los payloads
/// exactamente como están guardados.
/// </summary>
public sealed class QdrantClient(IHttpClientFactory httpFactory, IQdrantConfig config) : IQdrantClient
{
    private const int ScrollPageSize = 256;

    public async Task<IReadOnlySet<string>> GetCollectionNamesAsync(CancellationToken ct = default)
    {
        var res = await SendAsync(HttpMethod.Get, "/collections", null, ct);
        var names = new HashSet<string>();
        foreach (var c in res?["collections"]?.AsArray() ?? [])
            if (c?["name"]?.GetValue<string>() is { } n) names.Add(n);
        return names;
    }

    public Task CreateCollectionAsync(string name, int vectorSize, string distance, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"/collections/{name}",
            new JsonObject { ["vectors"] = new JsonObject { ["size"] = vectorSize, ["distance"] = distance } }, ct);

    public Task UpsertAsync(string collection, string id, IReadOnlyList<float> vector, JsonObject payload, CancellationToken ct = default)
    {
        var point = new JsonObject { ["id"] = id, ["vector"] = ToJsonArray(vector), ["payload"] = payload.DeepClone() };
        return SendAsync(HttpMethod.Put, $"/collections/{collection}/points?wait=true",
            new JsonObject { ["points"] = new JsonArray(point) }, ct);
    }

    public async Task<IReadOnlyList<QdrantPoint>> SearchAsync(string collection, IReadOnlyList<float> vector, int limit,
        CancellationToken ct = default)
    {
        var res = await SendAsync(HttpMethod.Post, $"/collections/{collection}/points/search",
            new JsonObject { ["vector"] = ToJsonArray(vector), ["limit"] = limit, ["with_payload"] = true }, ct);
        return (res?.AsArray() ?? []).Select(p => new QdrantPoint(
            IdOf(p!["id"]),
            p["payload"] as JsonObject ?? [],
            p["score"]!.GetValue<double>())).ToList();
    }

    public async Task<IReadOnlyList<QdrantPoint>> ScrollAsync(string collection, QdrantFilter? filter = null,
        CancellationToken ct = default)
    {
        var points = new List<QdrantPoint>();
        JsonNode? offset = null;
        do
        {
            var body = new JsonObject
            {
                ["limit"] = ScrollPageSize,
                ["with_payload"] = true,
                ["with_vector"] = false,
            };
            if (offset is not null) body["offset"] = offset.DeepClone();
            if (filter is { IsEmpty: false }) body["filter"] = filter.ToJson();
            var res = await SendAsync(HttpMethod.Post, $"/collections/{collection}/points/scroll", body, ct);
            foreach (var p in res?["points"]?.AsArray() ?? [])
                points.Add(new QdrantPoint(IdOf(p!["id"]), (p["payload"] as JsonObject)?.DeepClone().AsObject() ?? []));
            offset = res?["next_page_offset"];
        } while (offset is not null);
        return points;
    }

    public async Task<QdrantPoint?> RetrieveAsync(string collection, string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out _)) return null;
        var res = await SendAsync(HttpMethod.Post, $"/collections/{collection}/points",
            new JsonObject { ["ids"] = new JsonArray(id), ["with_payload"] = true }, ct);
        var p = res?.AsArray().FirstOrDefault();
        return p is null ? null : new QdrantPoint(IdOf(p["id"]), (p["payload"] as JsonObject)?.DeepClone().AsObject() ?? []);
    }

    public Task SetPayloadAsync(string collection, string id, JsonObject payload, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/collections/{collection}/points/payload?wait=true",
            new JsonObject { ["payload"] = payload.DeepClone(), ["points"] = new JsonArray(id) }, ct);

    public Task DeleteAsync(string collection, string id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/collections/{collection}/points/delete?wait=true",
            new JsonObject { ["points"] = new JsonArray(id) }, ct);

    // ─── Transporte ──────────────────────────────────────────────────────────

    private static JsonArray ToJsonArray(IReadOnlyList<float> vector)
    {
        var arr = new JsonArray();
        foreach (var f in vector) arr.Add(f);
        return arr;
    }

    private static string IdOf(JsonNode? id) =>
        id is JsonValue v && v.TryGetValue<string>(out var s) ? s : id?.ToJsonString() ?? "";

    private async Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonObject? body, CancellationToken ct)
    {
        var http = httpFactory.CreateClient(HttpClientNames.Qdrant);
        using var req = new HttpRequestMessage(method, config.QdrantUrl + path);
        if (body is not null)
            req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        HttpResponseMessage res;
        try
        {
            res = await http.SendAsync(req, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new DataStoreUnavailableException($"{ex.Message} ({config.QdrantUrl})", ex);
        }
        using (res)
        {
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == HttpStatusCode.NotFound && path.Contains("/points"))
                    throw new DataStoreUnavailableException($"Colección inexistente: {path}");
                throw new InvalidOperationException($"Qdrant {(int)res.StatusCode} en {method} {path}: {text}");
            }
            return JsonNode.Parse(text)?["result"];
        }
    }
}
