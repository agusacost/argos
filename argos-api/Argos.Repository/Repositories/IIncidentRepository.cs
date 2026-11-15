using Argos.Entities;

namespace Argos.Repository.Repositories;

public interface IIncidentRepository : IRepository<Incident>
{
    /// <summary>Incidentes más parecidos al texto (búsqueda vectorial); con <paramref name="tipoAlerta"/>, solo de ese tipo.</summary>
    Task<IReadOnlyList<ScoredIncident>> SearchSimilarAsync(string text, int limit, string? tipoAlerta = null);

    /// <summary>El incidente guardado con el mismo id natural (tipo + resumen [+ detected_at]), o null.</summary>
    Task<Incident?> GetExistingAsync(Incident incident);

    /// <summary>Incidentes que cumplen los filtros por campo (estado, estados, tipo, fuente).</summary>
    Task<IReadOnlyList<Incident>> FindAsync(IncidentFilter filter);
}
