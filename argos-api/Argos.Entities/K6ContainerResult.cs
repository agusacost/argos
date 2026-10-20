namespace Argos.Entities;

/// <summary>Salida de un contenedor k6 terminado: exit code y logs (stdout + stderr).</summary>
public sealed record K6ContainerResult(int ExitCode, string Logs);
