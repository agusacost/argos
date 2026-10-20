namespace Argos.Entities;

/// <summary>Escenario de k6 disponible en la vista "Pruebas de Carga".</summary>
public sealed class LoadTestScenario
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Descripcion { get; init; } = "";
    public string Perfil { get; init; } = "";
    public string DuracionAprox { get; init; } = "";
    public string Archivo { get; init; } = "";

    /// <summary>Código fuente del script (se completa al listar).</summary>
    public string Code { get; init; } = "";

    public LoadTestScenario WithCode(string code) => new()
    {
        Id = Id,
        Name = Name,
        Descripcion = Descripcion,
        Perfil = Perfil,
        DuracionAprox = DuracionAprox,
        Archivo = Archivo,
        Code = code,
    };
}
