using System.ComponentModel.DataAnnotations;
using Argos.Entities.Dtos;

namespace Argos.Tests.Knowledge;

/// <summary>F-06: misma validación que aplica el lector de bodies de la API (primer error → 422 con ese detail).</summary>
public sealed class KnowledgeEntryRequestTests
{
    private static string? FirstError(KnowledgeEntryRequest request)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true)
            ? null
            : results[0].ErrorMessage;
    }

    [Fact]
    public void Valid_WithStepsOnly() =>
        Assert.Null(FirstError(new KnowledgeEntryRequest { TipoAlerta = "X", Resumen = "r", Pasos = ["uno"] }));

    [Fact]
    public void Valid_WithDocumentOnly_AndSeverity() =>
        Assert.Null(FirstError(new KnowledgeEntryRequest
        {
            TipoAlerta = "X", Resumen = "r", Documento = "# Doc", Severity = "critical",
        }));

    [Theory]
    [InlineData(null, "r", "Campo 'tipo_alerta' requerido")]
    [InlineData("  ", "r", "Campo 'tipo_alerta' requerido")]
    [InlineData("X", null, "Campo 'resumen' requerido")]
    [InlineData("X", "", "Campo 'resumen' requerido")]
    public void MissingRequiredFields(string? tipo, string? resumen, string expected) =>
        Assert.Equal(expected, FirstError(new KnowledgeEntryRequest { TipoAlerta = tipo, Resumen = resumen, Pasos = ["p"] }));

    [Fact]
    public void WithoutStepsNorDocument_IsRejected()
    {
        var request = new KnowledgeEntryRequest { TipoAlerta = "X", Resumen = "r", Pasos = ["", "  ", null], Documento = " " };

        Assert.Equal("Hay que informar al menos un paso de resolución o el documento", FirstError(request));
    }

    [Fact]
    public void InvalidSeverity_IsRejected() =>
        Assert.Equal("Campo 'severity': debe ser 'warning' o 'critical'",
            FirstError(new KnowledgeEntryRequest { TipoAlerta = "X", Resumen = "r", Pasos = ["p"], Severity = "info" }));

    [Fact]
    public void CleansStepsAndDocument()
    {
        var request = new KnowledgeEntryRequest { Pasos = [" a ", "", null, "b"], Documento = "  \n# Doc\n " };

        Assert.Equal(["a", "b"], request.CleanPasos);
        Assert.Equal("# Doc", request.CleanDocumento);
    }
}
