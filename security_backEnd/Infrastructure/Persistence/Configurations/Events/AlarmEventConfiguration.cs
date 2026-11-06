using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Events;

public sealed class AlarmEventConfiguration : IEntityTypeConfiguration<AlarmEvent>
{
    public void Configure(EntityTypeBuilder<AlarmEvent> builder)
    {
        builder.ToTable("alarm_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.EventUid).HasColumnName("event_uid").IsRequired();
        builder.Property(x => x.OwnershipId).HasColumnName("ownership_id");
        builder.Property(x => x.OwnershipVersion).HasColumnName("ownership_version").IsRequired();
        builder.Property(x => x.SensorId).HasColumnName("sensor_id");
        builder.Property(x => x.SensorKeySnapshot).HasColumnName("sensor_key_snapshot").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).IsRequired();
        builder.Property(x => x.OccurredDeviceAt).HasColumnName("occurred_device_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.EndedDeviceAt).HasColumnName("ended_device_at").HasColumnType("timestamptz");
        builder.Property(x => x.EndedReceivedAt).HasColumnName("ended_received_at").HasColumnType("timestamptz");
        builder.Property(x => x.MutedAt).HasColumnName("muted_at").HasColumnType("timestamptz");
        builder.Property(x => x.ClockTrusted).HasColumnName("clock_trusted").IsRequired();
        builder.Property(x => x.SignalSummary).HasColumnName("signal_summary").HasColumnType("jsonb");
        builder.Property(x => x.WasBuffered).HasColumnName("was_buffered").IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.DeviceId, x.EventUid }).IsUnique().HasDatabaseName("ux_alarm_event_uid");
        builder.HasIndex(x => new { x.DeviceId, x.Id }).IsUnique().HasDatabaseName("ux_alarm_events_device_id_id");
        builder.HasIndex(x => new { x.DeviceId, x.ReceivedAt }).IsDescending(false, true).HasDatabaseName("ix_alarm_events_device_received");
        builder.HasIndex(x => new { x.OwnershipId, x.ReceivedAt }).IsDescending(false, true).HasDatabaseName("ix_alarm_events_ownership_received");

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeviceOwnership>()
            .WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.OwnershipId })
            .HasPrincipalKey(x => new { x.DeviceId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<DeviceSensor>()
            .WithMany()
            .HasForeignKey(x => new { x.DeviceId, x.SensorId })
            .HasPrincipalKey(x => new { x.DeviceId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_alarm_events_status", $"status IN ({DbStatusCodes.AlarmEvent.SqlInList})");
            t.HasCheckConstraint("ck_alarm_events_ownership_version_nonnegative", "ownership_version >= 0");
        });
    }
}
