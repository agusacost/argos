namespace Argos.Entities.Dtos;

/// <summary>Datos públicos de un usuario (sin el hash de la contraseña).</summary>
public class UserDto
{
    public string? Id { get; init; }
    public string Email { get; init; } = "";
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";

    public static UserDto From(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role,
    };
}
