namespace Argos.Common.Configuration;

/// <summary>Contenedor k6 hermano: imagen, red, volúmenes y rutas compartidas.</summary>
public interface ILoadTestConfig
{
    string K6Image { get; }
    string K6Network { get; }
    string K6ScriptsVolume { get; }
    string K6ResultsVolume { get; }
    string K6DefaultBaseUrl { get; }
    string K6TestsDir { get; }
    string K6DataDir { get; }
    string K6ScriptsDir { get; }
    string K6ResultsDir { get; }
    string DockerHost { get; }
}
