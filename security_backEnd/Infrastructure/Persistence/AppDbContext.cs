using Domain.Entities.Auth;
using Domain.Entities.Commands;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Domain.Entities.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// سياق EF Core لنظام الأمان الذكي — هوية Guid واحدة دون جداول مستخدمين مكررة.
/// </summary>
public sealed class AppDbContext
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceOwnership> DeviceOwnerships => Set<DeviceOwnership>();
    public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();
    public DbSet<DeviceSensor> DeviceSensors => Set<DeviceSensor>();
    public DbSet<DeviceSettings> DeviceSettings => Set<DeviceSettings>();
    public DbSet<DeviceState> DeviceStates => Set<DeviceState>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<CommandStatusHistory> CommandStatusHistory => Set<CommandStatusHistory>();
    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();
    public DbSet<DeviceFault> DeviceFaults => Set<DeviceFault>();
    public DbSet<EventLossReport> EventLossReports => Set<EventLossReport>();
    public DbSet<PushToken> PushTokens => Set<PushToken>();
    public DbSet<AdminAlert> AdminAlerts => Set<AdminAlert>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<AuthRefreshTokenHistory> AuthRefreshTokenHistory => Set<AuthRefreshTokenHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<IdentityUserClaim<Guid>>(e =>
        {
            e.ToTable("user_claims");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ClaimType).HasColumnName("claim_type");
            e.Property(x => x.ClaimValue).HasColumnName("claim_value");
        });

        builder.Entity<IdentityUserLogin<Guid>>(e =>
        {
            e.ToTable("user_logins");
            e.Property(x => x.LoginProvider).HasColumnName("login_provider");
            e.Property(x => x.ProviderKey).HasColumnName("provider_key");
            e.Property(x => x.ProviderDisplayName).HasColumnName("provider_display_name");
            e.Property(x => x.UserId).HasColumnName("user_id");
        });

        builder.Entity<IdentityUserToken<Guid>>(e =>
        {
            e.ToTable("user_tokens");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.LoginProvider).HasColumnName("login_provider");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Value).HasColumnName("value");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(e =>
        {
            e.ToTable("role_claims");
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.Property(x => x.ClaimType).HasColumnName("claim_type");
            e.Property(x => x.ClaimValue).HasColumnName("claim_value");
        });

        // الوثيقة: منع CASCADE الذي يمحو الملكيات/الحوادث/الأوامر/التدقيق عند حذف مستخدم أو جهاز.
        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
