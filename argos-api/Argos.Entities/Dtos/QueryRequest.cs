using System.ComponentModel.DataAnnotations;

namespace Argos.Entities.Dtos;

/// <summary>Body de <c>POST /query</c> (n8n).</summary>
public sealed class QueryRequest : IRequestDto
{
    public const int DefaultTopK = 3;

    [Required(ErrorMessage = "Campo 'text' requerido")]
    [MinLength(1, ErrorMessage = "Campo 'text': mínimo 1 caracteres")]
    public string? Text { get; set; }

    [Range(1, 20, ErrorMessage = "Campo 'top_k': debe estar entre 1 y 20")]
    public int? TopK { get; set; }
}
