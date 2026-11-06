using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Commands;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Commands;

public sealed class DeviceCommandConfiguration : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("device_commands");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.OwnershipId).HasColumnName("ownership_id").IsRequired();
        builder.Property(x => x.OwnershipVersion).HasColumnName("ownership_version").IsRequired();
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();
        builder.Property(x => x.CommandSequence).HasColumnName("command_sequence").IsRequired();
        builder.Property(x => x.CommandType).HasColumnName("command_type").HasMaxLength(16).IsRequired();
        builder.Property(x => x.AlarmEventId).HasColumnName("alarm_event_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");
        builder.Property(x => x.AcknowledgedAt).HasColumnName("acknowledged_at").HasColumnType("timestamptz");
        builder.Property(x => x.RejectCode).HasColumnName("reject_code").HasMaxLength(80);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.ResultStateSequence).HasColumnName("result_state_sequence");
        builder.Property(x => x.TargetAlarmEventUid).HasColumnName("target_alarm_event_uid");
        builder.Property(x => x.ObservedStateSequence).HasColumnName("observed_state_sequence").IsRequired();
        builder.Property(x => x.ReconciledAt).HasColumnName("reconciled_at").HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.DeviceId, x.CommandSequence }).IsUnique().HasDatabaseName("ux_device_commands_device_sequence");
        builder.HasIndex(x => x.DeviceId)
            .IsUnique()
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ux_device_pending_command");
        builder.HasIndex(x => new { x.RequestedByUserId, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_device_commands_idempotency");
        builder.HasIndex(x => new { x.DeviceId, x.CreatedAt }).IsDescending(false, true).HasDatabaseName("ix_device_commands_device_created");
        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ix_device_commands_pending_expires");
        builder.HasIndex(x => new { x.OwnershipId, x.CreatedAt }).IsDescending(false, true).HasDatabaseName("ix_device_commands_ownership_created");

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK مركّب يمنع ربط أمر بملكية جهاز آخر.
        builder.HasOne<DeviceOwnership>()
            .WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.OwnershipId })
            .HasPrincipalKey(x => new { x.DeviceId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AlarmEvent>()
            .WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.AlarmEventId })
            .HasPrincipalKey(x => new { x.DeviceId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_device_commands_type",
                $"command_type IN ({DbStatusCodes.CommandType.SqlInList})");
            t.HasCheckConstraint(
                "ck_device_commands_status",
                $"status IN ({DbStatusCodes.CommandStatus.SqlInList})");
            t.HasCheckConstraint("ck_device_commands_expires_after_created", "expires_at > created_at");
            t.HasCheckConstraint(
                "ck_device_commands_mute_requires_target_uid",
                "command_type <> 'MUTE_ALARM' OR target_alarm_event_uid IS NOT NULL");
        });
    }
}
