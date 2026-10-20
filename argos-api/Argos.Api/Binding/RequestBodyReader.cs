using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Argos.Common.Exceptions;
using Argos.Common.Serialization;

namespace Argos.Api.Binding;

/// <summary>
/// Contrato de los bodies de entrada: campos desconocidos se ignoran,
/// null equivale a ausente, los nombres son snake_case exactos y un tipo inválido responde 422.
/// </summary>
public sealed class RequestBodyReader : IRequestBodyReader
{
    // Decodificación estricta: System.Text.Json reemplaza los bytes inválidos en silencio.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly JsonSerializerOptions _options;

    public RequestBodyReader()
    {
        _options = JsonDefaults.Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _options.Converters.Add(new LenientInt32Converter());
        _options.Converters.Add(new LenientBooleanConverter());
    }

    public async Task<object> ReadAsync(HttpRequest request, Type dtoType)
    {
        var json = Parse(await ReadTextAsync(request));
        object dto;
        try
        {
            dto = json.Deserialize(dtoType, _options) ?? throw new InvalidInputException("Se esperaba un objeto JSON");
        }
        catch (JsonException ex)
        {
            throw new InvalidInputException($"Campo '{FieldOf(ex.Path)}': tipo inválido");
        }
        Validate(dto);
        return dto;
    }

    private static async Task<string> ReadTextAsync(HttpRequest request)
    {
        try
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            return StrictUtf8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
        }
        catch (Exception ex)
        {
            // Body ilegible (p.ej. UTF-8 inválido).
            throw new BadRequestException("There was an error parsing the body", ex);
        }
    }

    private static JsonObject Parse(string text)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            throw new InvalidInputException("JSON inválido");
        }
        return node as JsonObject ?? throw new InvalidInputException("Se esperaba un objeto JSON");
    }

    private static void Validate(object dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true))
            throw new InvalidInputException(results[0].ErrorMessage ?? "Body inválido");
    }

    private static string FieldOf(string? path) => (path ?? "$").TrimStart('$', '.');
}
