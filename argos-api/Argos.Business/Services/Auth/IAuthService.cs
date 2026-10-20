using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Auth;

public interface IAuthService
{
    /// <summary>Token + usuario; 401 "Credenciales inválidas" si el email o la contraseña no coinciden.</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>Datos del usuario del token; 404 si ya no existe.</summary>
    Task<UserDto> GetProfileAsync(CurrentUser currentUser);
}
