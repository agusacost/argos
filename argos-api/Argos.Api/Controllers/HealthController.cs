using Argos.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Argos.Api.Controllers;

[Route("health")]
public sealed class HealthController : ArgosControllerBase
{
    [HttpGet]
    public HealthResponse Get() => new("ok");
}
