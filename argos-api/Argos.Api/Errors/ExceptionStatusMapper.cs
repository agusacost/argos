using Argos.Common.Exceptions;

namespace Argos.Api.Errors;

public sealed class ExceptionStatusMapper : IExceptionStatusMapper
{
    public ErrorMapping? Map(Exception exception) => exception switch
    {
        BadRequestException e => new(StatusCodes.Status400BadRequest, e.Detail),
        UnauthorizedException e => new(StatusCodes.Status401Unauthorized, e.Detail),
        ForbiddenException e => new(StatusCodes.Status403Forbidden, e.Detail),
        NotFoundException e => new(StatusCodes.Status404NotFound, e.Detail),
        ConflictException e => new(StatusCodes.Status409Conflict, e.Detail),
        InvalidInputException e => new(StatusCodes.Status422UnprocessableEntity, e.Detail),
        BadHttpRequestException e => new(StatusCodes.Status422UnprocessableEntity, e.Message),
        UpstreamServiceException e => new(StatusCodes.Status502BadGateway, e.Detail),
        DataStoreUnavailableException e => new(StatusCodes.Status503ServiceUnavailable, $"Qdrant no disponible: {e.Detail}"),
        _ => null,
    };
}
