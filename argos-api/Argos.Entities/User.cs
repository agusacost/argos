namespace Argos.Entities;

/// <summary>Usuario del dashboard (colección <c>argos_users</c>).</summary>
public sealed class User : Entity
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = UserRoles.Operador;
    public string? PasswordHash { get; set; }
    public string? CreatedAt { get; set; }

    public void Rename(string? name)
    {
        if (name is not null) Name = name;
    }

    public void ChangeRole(string? role)
    {
        if (role is not null) Role = role;
    }

    public void ChangePasswordHash(string? passwordHash)
    {
        if (passwordHash is not null) PasswordHash = passwordHash;
    }
}
