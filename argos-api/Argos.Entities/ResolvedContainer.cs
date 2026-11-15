namespace Argos.Entities;

/// <summary>
/// Contenedor al que corresponde un host (IP, nombre o alias de red) reportado por Nginx o por una alerta.
/// <see cref="Status"/> es el estado que informa Docker, por ejemplo "Up 2 hours" o "Exited (0) 3 minutes ago".
/// </summary>
public sealed record ResolvedContainer(string Host, string Name, string Status);
