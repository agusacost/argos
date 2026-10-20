using Argos.Business.Services.Users;
using Argos.Entities;
using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

/// <summary>Administración de usuarios (solo Admin).</summary>
[Authorize(Roles = UserRoles.Admin)]
[Route("users")]
public sealed class UsersController(IUserService users) : ArgosControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserDetailDto>> List() => users.ListAsync();

    [HttpPost]
    public async Task<ObjectResult> Create(CreateUserRequest request) => Created(await users.CreateAsync(request));

    [HttpPatch("{userId}")]
    public Task<UserDetailDto> Update(string userId, UpdateUserRequest request) => users.UpdateAsync(userId, request);

    [HttpDelete("{userId}")]
    public async Task<NoContentResult> Delete(string userId)
    {
        await users.DeleteAsync(userId, RequiredUser);
        return NoContent();
    }
}
