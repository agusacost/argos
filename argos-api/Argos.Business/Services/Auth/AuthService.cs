using Argos.Business.Security;
using Argos.Common.Exceptions;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;

namespace Argos.Business.Services.Auth;

public sealed class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens) : IAuthService
{
    private const string TokenType = "bearer";

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await users.GetByEmailAsync(request.Email!);
        if (user is null || !hasher.Verify(request.Password!, user.PasswordHash))
            throw new UnauthorizedException("Credenciales inválidas");
        return new LoginResponse(tokens.CreateToken(user), TokenType, UserDto.From(user));
    }

    public async Task<UserDto> GetProfileAsync(CurrentUser currentUser)
    {
        var user = await users.GetByEmailAsync(currentUser.Email) ?? throw new NotFoundException("Usuario no encontrado");
        return UserDto.From(user);
    }
}
