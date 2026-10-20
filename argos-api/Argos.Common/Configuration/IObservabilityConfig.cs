namespace Argos.Common.Configuration;

/// <summary>URLs del stack de observabilidad y de los servicios que se sondean.</summary>
public interface IObservabilityConfig
{
    string PrometheusUrl { get; }
    string LokiUrl { get; }
    string AlertmanagerUrl { get; }
    string N8nUrl { get; }
    string NginxStatusUrl { get; }
    string DotnetApiUrl { get; }
    string RagSelfUrl { get; }
    string QdrantReadyUrl { get; }
}
