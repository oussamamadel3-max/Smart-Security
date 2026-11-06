using System.Security.Claims;
using API.Filters;
using API.Infrastructure;
using Application.Common.Permissions;
using Application.Dtos.AdminDtos;
using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.AdminControllers;

[ApiController]
[Authorize]
[Route("api/admin/owners")]
[ServiceFilter(typeof(NoStoreResultFilter))]
public sealed class OwnersController : ControllerBase
{
    private readonly IOwnerAdminService _owners;
    private readonly IPermissionGate _gate;

    public OwnersController(IOwnerAdminService owners, IPermissionGate gate)
    {
        _owners = owners;
        _gate = gate;
    }

    [HttpGet]
    public async Task<IResult> List(CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.View);
        var items = await _owners.ListOwnersAsync(ct);
        return ApiResponse.Success("قائمة المالكين.", items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> Get(Guid id, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.View);
        var item = await _owners.GetOwnerAsync(id, ct);
        return ApiResponse.Success("تفاصيل المالك.", item);
    }

    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateOwnerRequestDto request, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.Manage);
        var dto = await _owners.CreateAsync(request, RequireUserId(), ct);
        return ApiResponse.Success("تم إنشاء حساب المالك.", dto, status: StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IResult> Update(Guid id, [FromBody] UpdateOwnerRequestDto request, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.Manage);
        var dto = await _owners.UpdateAsync(id, request, RequireUserId(), ct);
        return ApiResponse.Success("تم تحديث حساب المالك.", dto);
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IResult> Disable(Guid id, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.Manage);
        await _owners.DisableAsync(id, RequireUserId(), ct);
        return ApiResponse.Success("تم تعطيل الحساب.");
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IResult> Archive(Guid id, CancellationToken ct)
    {
        await _gate.EnsureAll(SystemPermissions.Users.Manage);
        await _owners.ArchiveAsync(id, RequireUserId(), ct);
        return ApiResponse.Success("تم أرشفة الحساب.");
    }

    private Guid RequireUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new UnauthenticatedUserException();
        return id;
    }
}
