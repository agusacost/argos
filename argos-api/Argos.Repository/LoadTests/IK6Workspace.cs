using Argos.Entities;

namespace Argos.Repository.LoadTests;

/// <summary>Archivos de k6: scripts de k6/tests, datasets de k6/data y el summary de cada corrida.</summary>
public interface IK6Workspace
{
    /// <summary>Código del script, o null si no está montado.</summary>
    string? ReadScript(string fileName);

    /// <summary>Copia el script y los datasets al volumen compartido con el contenedor k6.</summary>
    void PrepareScript(string fileName);

    /// <summary>Métricas del <c>--summary-export</c> de la corrida, o null si no existe o no se puede leer.</summary>
    LoadTestSummary? ReadSummary(string runId);
}
