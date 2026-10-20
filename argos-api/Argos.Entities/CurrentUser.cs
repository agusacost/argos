using System.Text.Json.Serialization;

namespace Argos.Entities;

/// <summary>Usuario autenticado según los claims del JWT (<c>sub</c>, <c>email</c>, <c>role</c>).</summary>
public sealed record CurrentUser(string Sub, string Email, string Role)
{
    [JsonIgnore]
    public bool IsAdmin => Role == UserRoles.Admin;
}
