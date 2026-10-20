namespace Argos.Entities.Dtos;

public sealed record LoginResponse(string AccessToken, string TokenType, UserDto User);
