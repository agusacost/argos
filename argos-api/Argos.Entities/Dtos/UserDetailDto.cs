namespace Argos.Entities.Dtos;

/// <summary>Usuario en la administración de usuarios: incluye la fecha de alta.</summary>
public sealed class UserDetailDto : UserDto
{
    public string? CreatedAt { get; init; }

    public static new UserDetailDto From(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
    };
}
