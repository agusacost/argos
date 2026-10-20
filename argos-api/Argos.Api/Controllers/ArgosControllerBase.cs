using System.Security.Claims;
using Argos.Api.Authentication;
using Argos.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Base de los controllers: usuario autenticado a partir de los claims del JWT.</summary>
public abstract class ArgosControllerBase : ControllerBase
{
    /// <summary>Usuario del token, o null si el request no trae un token válido.</summary>
    protected CurrentUser? AuthenticatedUser =>
        User.Identity?.IsAuthenticated == true
            ? new CurrentUser(Claim(ArgosAuthenticationDefaults.SubjectClaim), Claim(ArgosAuthenticationDefaults.EmailClaim),
                Claim(ArgosAuthenticationDefaults.RoleClaim))
            : null;

    /// <summary>Usuario de un endpoint con <c>[Authorize]</c> (la autorización ya garantizó que existe).</summary>
    protected CurrentUser RequiredUser =>
        AuthenticatedUser ?? throw new InvalidOperationException("Endpoint sin [Authorize]: no hay usuario autenticado");

    protected ObjectResult Created<T>(T value) => StatusCode(StatusCodes.Status201Created, value);

    private string Claim(string type) => User.FindFirstValue(type) ?? "";
}
