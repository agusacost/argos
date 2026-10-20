using Argos.Entities;

namespace Argos.Repository.LoadTests;

/// <summary>Contenedor <c>grafana/k6</c> hermano, lanzado vía el Docker socket.</summary>
public interface IK6ContainerRunner
{
    /// <summary>Lanza <see cref="Argos.Common.Exceptions.ExternalServiceException"/> si Docker no responde.</summary>
    Task EnsureAvailableAsync();

    /// <summary>Crea y arranca el contenedor (descarga la imagen si falta). Devuelve su id.</summary>
    Task<string> StartAsync(string runId, string scriptFile, string baseUrl);

    /// <summary>Espera a que termine y devuelve exit code + logs.</summary>
    Task<K6ContainerResult> WaitAsync(string containerId);

    /// <summary>Elimina el contenedor (sin error si ya no existe).</summary>
    Task RemoveAsync(string containerId);

    /// <summary>Mata y elimina el contenedor (sin error si ya terminó).</summary>
    Task StopAsync(string containerId);
}
