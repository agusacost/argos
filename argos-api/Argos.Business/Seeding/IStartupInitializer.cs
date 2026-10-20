namespace Argos.Business.Seeding;

/// <summary>Inicialización al arrancar la API.</summary>
public interface IStartupInitializer
{
    /// <summary>No lanza: si Qdrant todavía no está listo, lo deja logueado y la API arranca igual.</summary>
    Task InitializeAsync();
}
