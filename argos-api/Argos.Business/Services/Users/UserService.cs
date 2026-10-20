using Argos.Business.Security;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;
using Argos.Entities.Dtos;
using Argos.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace Argos.Business.Services.Users;

public sealed class UserService(
    IUserRepository users,
    IPasswordHasher hasher,
    IClock clock,
    IAuthConfig config,
    ILogger<UserService> log) : IUserService
{
    private const string AdminName = "Administrador";

    public async Task<IReadOnlyList<UserDetailDto>> ListAsync() =>
        (await users.ListAsync()).Select(UserDetailDto.From).ToList();

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        if (await users.GetByEmailAsync(request.Email!) is not null)
            throw new ConflictException("Email ya registrado");
        var user = await CreateUserAsync(request.Email!, request.Name!, request.Role ?? UserRoles.Operador, request.Password!);
        return UserDto.From(user);
    }

    public async Task<UserDetailDto> UpdateAsync(string userId, UpdateUserRequest request)
    {
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("Usuario no encontrado");
        user.Rename(request.Name);
        user.ChangeRole(request.Role);
        user.ChangePasswordHash(request.Password is null ? null : hasher.Hash(request.Password));
        await users.UpdateAsync(user);
        return UserDetailDto.From(user);
    }

    public async Task DeleteAsync(string userId, CurrentUser currentUser)
    {
        if (currentUser.Sub == userId) throw new BadRequestException("No podés eliminarte a vos mismo");
        if (!await users.DeleteAsync(userId)) throw new NotFoundException("Usuario no encontrado");
    }

    public async Task EnsureAdminAsync()
    {
        if (await users.GetByEmailAsync(config.AdminEmail) is not null) return;
        await CreateUserAsync(config.AdminEmail, AdminName, UserRoles.Admin, config.AdminPassword);
        log.LogInformation("[meta_store] Admin seed creado: {Email}", config.AdminEmail);
    }

    private Task<User> CreateUserAsync(string email, string name, string role, string password) =>
        users.SaveAsync(new User
        {
            Email = email,
            Name = name,
            Role = role,
            PasswordHash = hasher.Hash(password),
            CreatedAt = clock.UtcNowNaive(),
        });
}
