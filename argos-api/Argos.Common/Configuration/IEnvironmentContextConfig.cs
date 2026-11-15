namespace Argos.Common.Configuration;

/// <summary>Descripción del entorno monitoreado que acompaña a los prompts.</summary>
public interface IEnvironmentContextConfig
{
    /// <summary>
    /// ARGOS_ENVIRONMENT_CONTEXT (default vacío): valor inicial del "Contexto del entorno" mientras nadie lo edite desde
    /// Configuración. Lo provee el despliegue del servicio monitoreado, no el código de Argos.
    /// </summary>
    string DefaultEnvironmentContext { get; }
}
