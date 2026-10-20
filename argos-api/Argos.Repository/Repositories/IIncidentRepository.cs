using Argos.Entities;

namespace Argos.Repository.Repositories;

public interface IIncidentRepository : IRepository<Incident>
{
    /// <summary>Incidentes más parecidos al texto (búsqueda vectorial).</summary>
    Task<IReadOnlyList<ScoredIncident>> SearchSimilarAsync(string text, int limit);

    /// <summary>Incidentes que cumplen los filtros por campo (estado, estados, tipo, fuente).</summary>
    Task<IReadOnlyList<Incident>> FindAsync(IncidentFilter filter);
}
