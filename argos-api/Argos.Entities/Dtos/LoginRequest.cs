using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

public sealed class LoginRequest : IRequestDto
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'email' requerido")]
    public string? Email { get; set; }

    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'password' requerido")]
    public string? Password { get; set; }
}
