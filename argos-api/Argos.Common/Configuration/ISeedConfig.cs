namespace Argos.Common.Configuration;

/// <summary>Carga inicial de runbooks e incidentes demo.</summary>
public interface ISeedConfig
{
    bool AutoSeed { get; }
    bool SeedDemo { get; }
    string KnowledgeDir { get; }
}
