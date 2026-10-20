using Argos.Entities;
using Argos.Entities.Dtos;

namespace Argos.Business.Services.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDetailDto>> ListAsync();

    /// <summary>409 si el email ya está registrado.</summary>
    Task<UserDto> CreateAsync(CreateUserRequest request);

    /// <summary>404 si no existe.</summary>
    Task<UserDetailDto> UpdateAsync(string userId, UpdateUserRequest request);

    /// <summary>400 si el admin intenta eliminarse a sí mismo; 404 si no existe.</summary>
    Task DeleteAsync(string userId, CurrentUser currentUser);

    /// <summary>Crea el admin inicial (ARGOS_ADMIN_EMAIL) si no existe.</summary>
    Task EnsureAdminAsync();
}
