namespace Argos.Entities.Dtos;

/// <summary>
/// Marca los bodies JSON de entrada. Contrato de lectura:
/// campos desconocidos se ignoran, null equivale a ausente y un tipo inválido responde 422.
/// </summary>
public interface IRequestDto
{
}
