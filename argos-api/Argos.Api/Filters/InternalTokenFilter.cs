using Argos.Common.Configuration;
using Argos.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Argos.Api.Filters;

/// <summary>Endpoints internos (n8n): si ARGOS_INTERNAL_TOKEN está definido, exige el header X-Argos-Token.</summary>
public sealed class InternalTokenFilter(IAuthConfig config) : IAuthorizationFilter
{
    private const string HeaderName = "X-Argos-Token";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = config.InternalToken;
        if (expected.Length > 0 && context.HttpContext.Request.Headers[HeaderName].ToString() != expected)
            throw new UnauthorizedException("Token interno inválido");
    }
}
