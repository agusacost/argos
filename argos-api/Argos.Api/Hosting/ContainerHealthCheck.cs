namespace Argos.Api.Hosting;

/// <summary>
/// Healthcheck del contenedor sin curl/wget en la imagen: <c>dotnet Argos.Api.dll healthcheck</c>
/// consulta <c>/health</c> de la instancia local y devuelve 0 si responde 2xx.
/// </summary>
public sealed class ContainerHealthCheck(string port)
{
    public const string Argument = "healthcheck";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<int> RunAsync()
    {
        using var http = new HttpClient { Timeout = Timeout };
        try
        {
            var res = await http.GetAsync($"http://127.0.0.1:{port}/health");
            return res.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
