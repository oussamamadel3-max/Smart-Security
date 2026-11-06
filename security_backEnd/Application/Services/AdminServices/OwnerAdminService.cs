using Application.Dtos.AdminDtos;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.AdminServices;

public sealed class OwnerAdminService : IOwnerAdminService
{
    private readonly UserManager<User> _users;
    private readonly RoleManager<Role> _roles;
    private readonly IAuthSessionRepository _sessions;
    private readonly IUnitOfWork _uow;
    private readonly IOwnerLookup _owners;
    private readonly IAuditLogService _audit;

    public OwnerAdminService(
        UserManager<User> users,
        RoleManager<Role> roles,
        IAuthSessionRepository sessions,
        IUnitOfWork uow,
        IOwnerLookup owners,
        IAuditLogService audit)
    {
        _users = users;
        _roles = roles;
        _sessions = sessions;
        _uow = uow;
        _owners = owners;
        _audit = audit;
    }

    public async Task<OwnerAccountResponseDto> CreateAsync(
        CreateOwnerRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        ValidateNames(request.UserName, request.FullName);
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new BadRequestException("كلمة المرور غير مقبولة.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                FullName = request.FullName.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };

            var create = await _users.CreateAsync(user, request.Password);
            if (!create.Succeeded)
                throw new BadRequestException(create.Errors.FirstOrDefault()?.Description ?? "تعذر إنشاء الحساب.");

            if (!await _roles.RoleExistsAsync(DbStatusCodes.RoleCodes.Owner))
                throw new ConflictException("دور OWNER غير مهيأ.");

            var roleResult = await _users.AddToRoleAsync(user, DbStatusCodes.RoleCodes.Owner);
            if (!roleResult.Succeeded)
                throw new BadRequestException("تعذر منح دور المالك.");

            await _audit.WriteAsync(
                actorUserId,
                AuditActions.OwnerCreated,
                AuditActions.TargetTypes.User,
                user.Id,
                DbStatusCodes.AuditResult.Success,
                new { userName = user.UserName, fullName = user.FullName },
                ct: ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ToDto(user);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OwnerAccountResponseDto> UpdateAsync(
        Guid ownerId,
        UpdateOwnerRequestDto request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        ValidateNames("placeholder", request.FullName);
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = await RequireOwnerAsync(ownerId);
            user.FullName = request.FullName.Trim();
            user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException(result.Errors.FirstOrDefault()?.Description ?? "تعذر التعديل.");

            await _audit.WriteAsync(
                actorUserId,
                AuditActions.OwnerUpdated,
                AuditActions.TargetTypes.User,
                user.Id,
                DbStatusCodes.AuditResult.Success,
                new { userName = user.UserName, fullName = user.FullName },
                ct: ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ToDto(user);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DisableAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = await RequireOwnerAsync(ownerId);
            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _users.UpdateSecurityStampAsync(user);
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException("تعذر تعطيل الحساب.");

            var now = DateTimeOffset.UtcNow;
            await _sessions.RevokeAllForUserAsync(user.Id, AuthStatusCodes.RevokeReason.UserDisabled, now, ct);
            await _audit.WriteAsync(
                actorUserId,
                AuditActions.OwnerDisabled,
                AuditActions.TargetTypes.User,
                user.Id,
                DbStatusCodes.AuditResult.Success,
                new { userName = user.UserName },
                ct: ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<AdminOwnerListItemDto>> ListOwnersAsync(CancellationToken ct = default)
    {
        var owners = await _users.GetUsersInRoleAsync(DbStatusCodes.RoleCodes.Owner);
        return owners
            .OrderByDescending(u => u.CreatedAt)
            .Select(ToAdminListItem)
            .ToList();
    }

    public async Task<AdminOwnerListItemDto> GetOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        var user = await RequireOwnerAsync(ownerId);
        return ToAdminListItem(user);
    }

    public async Task ArchiveAsync(Guid ownerId, Guid actorUserId, CancellationToken ct = default)
    {
        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            var user = await RequireOwnerAsync(ownerId);
            if (await _owners.HasOpenDeviceOwnershipAsync(ownerId, ct))
                throw new ConflictException("لا يمكن أرشفة حساب مرتبط بأجهزة قبل معالجة الارتباط.", "OWNER_HAS_DEVICES");

            var now = DateTimeOffset.UtcNow;
            user.IsActive = false;
            user.DeletedAt = now;
            user.UpdatedAt = now;
            await _users.UpdateSecurityStampAsync(user);
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BadRequestException("تعذر أرشفة الحساب.");

            await _sessions.RevokeAllForUserAsync(user.Id, AuthStatusCodes.RevokeReason.UserArchived, now, ct);
            await _audit.WriteAsync(
                actorUserId,
                AuditActions.OwnerArchived,
                AuditActions.TargetTypes.User,
                user.Id,
                DbStatusCodes.AuditResult.Success,
                new { userName = user.UserName },
                ct: ct);
            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<User> RequireOwnerAsync(Guid ownerId)
    {
        var user = await _users.FindByIdAsync(ownerId.ToString())
            ?? throw new NotFoundException("الحساب غير موجود.");

        if (!await _users.IsInRoleAsync(user, DbStatusCodes.RoleCodes.Owner))
            throw new NotFoundException("الحساب غير موجود.");

        return user;
    }

    private static void ValidateNames(string userName, string fullName)
    {
        if (userName != "placeholder" && (string.IsNullOrWhiteSpace(userName) || userName.Trim().Length > 120))
            throw new BadRequestException("اسم المستخدم غير صالح.");
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 200)
            throw new BadRequestException("الاسم الكامل غير صالح.");
    }

    private static OwnerAccountResponseDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        DeletedAt = user.DeletedAt,
        CreatedAt = user.CreatedAt
    };

    private static AdminOwnerListItemDto ToAdminListItem(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        DeletedAt = user.DeletedAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
