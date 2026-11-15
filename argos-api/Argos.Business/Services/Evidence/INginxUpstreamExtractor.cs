namespace Argos.Business.Services.Evidence;

/// <summary>Hosts upstream que Nginx reporta en sus logs como origen de los errores.</summary>
public interface INginxUpstreamExtractor
{
    /// <param name="upstreamAddrs">Valores de <c>upstream_addr</c> del access.log en respuestas 5xx, con su cantidad.</param>
    /// <param name="errorLines">Líneas del error.log de la ventana.</param>
    IReadOnlyList<string> Extract(IReadOnlyList<(string Addr, long Count)> upstreamAddrs, IReadOnlyList<string> errorLines);
}
