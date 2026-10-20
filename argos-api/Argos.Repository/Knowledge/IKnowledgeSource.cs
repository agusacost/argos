using Argos.Entities;

namespace Argos.Repository.Knowledge;

/// <summary>Runbooks de incidentes conocidos que se cargan al arrancar.</summary>
public interface IKnowledgeSource
{
    IReadOnlyList<Incident> LoadRunbooks();
}
