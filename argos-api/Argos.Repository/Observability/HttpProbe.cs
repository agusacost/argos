using System.Diagnostics;
using Argos.Entities;
using Argos.Repository.Http;

namespace Argos.Repository.Observability;

/// <summary>Sondeo con timeout corto (4 s). El detalle es el cuerpo (si el status es ≥300) o el error.</summary>
public sealed class HttpProbe(IHttpClientFactory httpFactory) : IHttpProbe
{
    private const int MaxDetailLength = 200;

    public async Task<ProbeResult> ProbeAsync(string url)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var r = await httpFactory.CreateClient(HttpClientNames.Probe).GetAsync(url);
            var code = (int)r.StatusCode;
            var detail = code >= 300 ? Truncate((await r.Content.ReadAsStringAsync()).Trim()) : "";
            return new ProbeResult(code, detail, sw.Elapsed);
        }
        catch (Exception ex)
        {
            return new ProbeResult(null, Truncate(ex.Message), sw.Elapsed);
        }
    }

    private static string Truncate(string s) => s.Length <= MaxDetailLength ? s : s[..MaxDetailLength];
}
