namespace Argos.Common.Configuration;

/// <summary>JWT, admin seed, token interno de n8n y CORS.</summary>
public interface IAuthConfig
{
    string JwtSecret { get; }
    string AdminEmail { get; }
    string AdminPassword { get; }
    string InternalToken { get; }
    string CorsOrigins { get; }
}
