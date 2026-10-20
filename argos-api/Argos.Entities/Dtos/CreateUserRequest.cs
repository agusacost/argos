using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

public sealed class CreateUserRequest : IRequestDto
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'email' requerido")]
    public string? Email { get; set; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'name' requerido")]
    public string? Name { get; set; }

    public string? Role { get; set; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'password' requerido")]
    public string? Password { get; set; }
}
