using API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Liveness probe for the API host.
/// </summary>
[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IResult Health()
        => ApiResponse.Success("API is healthy.");
}
