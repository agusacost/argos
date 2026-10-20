using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Argos.Common.Time;
using Argos.Entities;

namespace Argos.Business.Security;

/// <summary>
/// JWT HS256 con <c>sub/email/role/exp</c> (12 h). Implementado a mano para controlar los mensajes de
/// error y aceptar secretos de cualquier largo.
/// </summary>
public sealed class JwtTokenService(IAuthConfig config, IClock clock) : ITokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    public string CreateToken(User user)
    {
        var header = new JsonObject { ["alg"] = "HS256", ["typ"] = "JWT" };
        var payload = new JsonObject
        {
            ["sub"] = user.Id,
            ["email"] = user.Email,
            ["role"] = user.Role,
            ["exp"] = clock.UtcNow.Add(Lifetime).ToUnixTimeSeconds(),
        };
        var signingInput = Base64Url.Encode(Encoding.UTF8.GetBytes(header.ToJsonString())) + "." +
                           Base64Url.Encode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        return signingInput + "." + Base64Url.Encode(Sign(signingInput));
    }

    public CurrentUser ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw Invalid();
        JsonObject header, payload;
        byte[] signature;
        try
        {
            header = JsonNode.Parse(Base64Url.Decode(parts[0])) as JsonObject ?? throw Invalid();
            payload = JsonNode.Parse(Base64Url.Decode(parts[1])) as JsonObject ?? throw Invalid();
            signature = Base64Url.Decode(parts[2]);
        }
        catch (UnauthorizedException) { throw; }
        catch (Exception) { throw Invalid(); }

        if (StringClaim(header, "alg") != "HS256") throw Invalid();
        if (!CryptographicOperations.FixedTimeEquals(signature, Sign(parts[0] + "." + parts[1]))) throw Invalid();

        if (payload["exp"] is not null)
        {
            if (payload["exp"] is not JsonValue exp || exp.GetValueKind() != JsonValueKind.Number) throw Invalid();
            if (exp.GetValue<double>() <= clock.UtcNow.ToUnixTimeSeconds())
                throw new UnauthorizedException("Token expirado");
        }
        return new CurrentUser(StringClaim(payload, "sub"), StringClaim(payload, "email"), StringClaim(payload, "role"));
    }

    private static UnauthorizedException Invalid() => new("Token inválido");

    private static string StringClaim(JsonObject claims, string key) => claims[key] switch
    {
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        null => "",
        var other => other.ToJsonString(),
    };

    private byte[] Sign(string input) =>
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(config.JwtSecret), Encoding.ASCII.GetBytes(input));
}
