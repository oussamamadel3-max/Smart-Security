using API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Legacy JSON liveness under /api/health (ApiResponse envelope).
/// Canonical ops probes: GET /health/live (process) and GET /health/ready (PostgreSQL).
/// </summary>
[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IResult Health()
        => ApiResponse.Success("API is healthy.");
}
