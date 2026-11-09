namespace Argos.Entities;

/// <summary>Valores del campo <c>kind</c> de una notificación.</summary>
public static class NotificationKinds
{
    /// <summary>Incidente que llegó por el pipeline o que es nuevo/crítico.</summary>
    public const string Incident = "incident";

    /// <summary>Un proveedor LLM dejó de responder o se recuperó.</summary>
    public const string LlmHealth = "llm_health";

    /// <summary>El análisis o la documentación de una alerta falló en todos los proveedores.</summary>
    public const string PipelineError = "pipeline_error";
}
