using Argos.Business.Services.Auth;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

[Route("auth")]
public sealed class AuthController(IAuthService auth) : ArgosControllerBase
{
    [HttpPost("login")]
    public Task<LoginResponse> Login(LoginRequest request) => auth.LoginAsync(request);

    [Authorize]
    [HttpGet("me")]
    public Task<UserDto> Me() => auth.GetProfileAsync(RequiredUser);
}
