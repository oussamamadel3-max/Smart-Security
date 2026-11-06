using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Devices;
using Domain.Entities.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Operations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        builder.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.TargetId).HasColumnName("target_id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id");
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(12).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");

        builder.HasIndex(x => new { x.DeviceId, x.OccurredAt }).IsDescending(false, true).HasDatabaseName("ix_audit_logs_device_occurred");
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAt }).IsDescending(false, true).HasDatabaseName("ix_audit_logs_actor_occurred");
        builder.HasIndex(x => new { x.Action, x.OccurredAt }).IsDescending(false, true).HasDatabaseName("ix_audit_logs_action_occurred");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_audit_logs_result", $"result IN ({DbStatusCodes.AuditResult.SqlInList})");
        });
    }
}
