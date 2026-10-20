namespace Argos.Api.Authentication;

public static class ArgosAuthenticationDefaults
{
    public const string Scheme = "ArgosJwt";

    // Claims del JWT de sesión.
    public const string SubjectClaim = "sub";
    public const string EmailClaim = "email";
    public const string RoleClaim = "role";

    public const string MissingCredentials = "Se requiere autenticación";
    public const string AdminRequired = "Se requiere rol Admin";
}
