namespace Argos.Entities.Dtos;

/// <summary>PATCH de usuario: solo se aplican los campos informados.</summary>
public sealed class UpdateUserRequest : IRequestDto
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Password { get; set; }
}
