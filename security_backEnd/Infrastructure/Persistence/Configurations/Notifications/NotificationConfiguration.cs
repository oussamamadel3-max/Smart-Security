using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Events;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.NotificationType).HasColumnName("notification_type").HasMaxLength(16).IsRequired();
        builder.Property(x => x.AlarmEventId).HasColumnName("alarm_event_id");
        builder.Property(x => x.DeviceFaultId).HasColumnName("device_fault_id");
        builder.Property(x => x.AdminAlertId).HasColumnName("admin_alert_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LastAttemptAt).HasColumnName("last_attempt_at").HasColumnType("timestamptz");
        builder.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamptz");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
        builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(80);

        builder.HasIndex(x => new { x.RecipientUserId, x.AlarmEventId })
            .IsUnique()
            .HasFilter("alarm_event_id IS NOT NULL")
            .HasDatabaseName("ux_notifications_recipient_alarm");
        builder.HasIndex(x => new { x.RecipientUserId, x.DeviceFaultId })
            .IsUnique()
            .HasFilter("device_fault_id IS NOT NULL")
            .HasDatabaseName("ux_notifications_recipient_fault");
        builder.HasIndex(x => new { x.RecipientUserId, x.AdminAlertId })
            .IsUnique()
            .HasFilter("admin_alert_id IS NOT NULL")
            .HasDatabaseName("ux_notifications_recipient_admin_alert");
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("ix_notifications_status_next_attempt");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AlarmEvent>()
            .WithMany()
            .HasForeignKey(x => x.AlarmEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<DeviceFault>()
            .WithMany()
            .HasForeignKey(x => x.DeviceFaultId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<AdminAlert>()
            .WithMany()
            .HasForeignKey(x => x.AdminAlertId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_notifications_type",
                $"notification_type IN ({DbStatusCodes.NotificationType.SqlInList})");
            t.HasCheckConstraint(
                "ck_notifications_status",
                $"status IN ({DbStatusCodes.NotificationStatus.SqlInList})");
            t.HasCheckConstraint(
                "ck_notifications_single_source",
                """
                (
                  (notification_type = 'ALARM' AND alarm_event_id IS NOT NULL AND device_fault_id IS NULL AND admin_alert_id IS NULL)
                  OR (notification_type = 'FAULT' AND device_fault_id IS NOT NULL AND alarm_event_id IS NULL AND admin_alert_id IS NULL)
                  OR (notification_type = 'ADMIN_ALERT' AND admin_alert_id IS NOT NULL AND alarm_event_id IS NULL AND device_fault_id IS NULL)
                )
                """);
        });
    }
}
