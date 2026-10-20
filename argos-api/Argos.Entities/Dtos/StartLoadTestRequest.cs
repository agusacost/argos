using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

public sealed class StartLoadTestRequest : IRequestDto
{
    [Required(AllowEmptyStrings = true, ErrorMessage = "Campo 'scenario' requerido")]
    public string? Scenario { get; set; }

    public string? BaseUrl { get; set; }
}
