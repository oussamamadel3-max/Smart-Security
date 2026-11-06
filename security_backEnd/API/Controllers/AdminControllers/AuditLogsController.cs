using API.Filters;
using API.Infrastructure;
using Application.Common.Permissions;
using Application.Dtos.AdminDtos;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.AdminControllers;

/// <summary>
/// Read-only admin audit log API (FR-033 / BR-019). No update/delete endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admin/audit-logs")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _audit;
    private readonly IPermissionGate _gate;

    public AuditLogsController(IAuditLogService audit, IPermissionGate gate)
    {
        _audit = audit;
        _gate = gate;
    }

    [HttpGet]
    public async Task<IResult> Query([FromQuery] AuditLogQueryDto query, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.AuditLogs.View);
        var page = await _audit.QueryAsync(query, ct);
        return ApiResponse.Success("سجل عمليات الإدارة.", page);
    }
}
