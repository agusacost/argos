using System.Security.Claims;
using System.Text.Encodings.Web;
using Argos.Api.Errors;
using Argos.Business.Security;
using Argos.Common.Exceptions;
using Argos.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Argos.Api.Authentication;

/// <summary>
/// Autenticación Bearer con los JWT de Argos. Responde 401/403 con <c>{"detail"}</c> y los mensajes
/// que muestra el dashboard ("Se requiere autenticación", "Token inválido", "Token expirado",
/// "Se requiere rol Admin"). En endpoints sin <c>[Authorize]</c> un token inválido se ignora.
/// </summary>
public sealed class ArgosAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokens,
    IErrorResponseWriter errors)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "bearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = BearerToken();
        if (token is null) return Task.FromResult(AuthenticateResult.NoResult());
        try
        {
            var user = tokens.ValidateToken(token);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(ToPrincipal(user), Scheme.Name)));
        }
        catch (UnauthorizedException ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Detail));
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var result = await HandleAuthenticateOnceSafeAsync();
        var detail = result.Failure?.Message ?? ArgosAuthenticationDefaults.MissingCredentials;
        await errors.WriteAsync(Context, StatusCodes.Status401Unauthorized, detail);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        errors.WriteAsync(Context, StatusCodes.Status403Forbidden, ArgosAuthenticationDefaults.AdminRequired);

    /// <summary>Token de <c>Authorization: Bearer ...</c>; null si falta o tiene otro formato.</summary>
    private string? BearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header)) return null;
        var parts = header.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !parts[0].Equals(BearerPrefix, StringComparison.OrdinalIgnoreCase) || parts[1].Length == 0)
            return null;
        return parts[1];
    }

    private ClaimsPrincipal ToPrincipal(CurrentUser user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ArgosAuthenticationDefaults.SubjectClaim, user.Sub),
                new Claim(ArgosAuthenticationDefaults.EmailClaim, user.Email),
                new Claim(ArgosAuthenticationDefaults.RoleClaim, user.Role),
            ],
            Scheme.Name,
            ArgosAuthenticationDefaults.EmailClaim,
            ArgosAuthenticationDefaults.RoleClaim);
        return new ClaimsPrincipal(identity);
    }
}
