namespace Argos.Business.Services.Observability;

/// <summary>Relaciona el tipo de alerta con el componente afectado y sus fuentes de datos.</summary>
public interface IAlertComponentResolver
{
    /// <summary>nginx, postgresql, redis, api, kubernetes, loki, host o unknown.</summary>
    string ComponentOf(string tipoAlerta);

    /// <summary>Valor del label <c>service</c> de Loki para el componente.</summary>
    string LokiServiceOf(string component);

    /// <summary>Expresión de Prometheus de la sparkline, o null si el componente no tiene métrica.</summary>
    string? PrometheusExpressionOf(string component);
}
