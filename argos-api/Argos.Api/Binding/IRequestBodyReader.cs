namespace Argos.Api.Binding;

/// <summary>Lee y valida el body JSON de un request contra un DTO de entrada.</summary>
public interface IRequestBodyReader
{
    /// <summary>
    /// 400 si el body no es UTF-8 válido; 422 si no es JSON, no es un objeto, algún campo tiene un tipo
    /// inválido o no cumple las validaciones del DTO.
    /// </summary>
    Task<object> ReadAsync(HttpRequest request, Type dtoType);
}
