using Domain.Constants;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceStateConfiguration : IEntityTypeConfiguration<DeviceState>
{
    public void Configure(EntityTypeBuilder<DeviceState> builder)
    {
        builder.ToTable("device_states");
        builder.HasKey(x => x.DeviceId);

        builder.Property(x => x.DeviceId).HasColumnName("device_id");
        builder.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").HasColumnType("timestamptz");
        builder.Property(x => x.LastStateAt).HasColumnName("last_state_at").HasColumnType("timestamptz");
        builder.Property(x => x.StateSequence).HasColumnName("state_sequence").IsRequired();
        builder.Property(x => x.ArmState).HasColumnName("arm_state").HasMaxLength(12).IsRequired();
        builder.Property(x => x.SirenState).HasColumnName("siren_state").HasMaxLength(12).IsRequired();
        builder.Property(x => x.HealthState).HasColumnName("health_state").HasMaxLength(12).IsRequired();
        builder.Property(x => x.HealthCode).HasColumnName("health_code").HasMaxLength(80);
        builder.Property(x => x.PowerSource).HasColumnName("power_source").HasMaxLength(12);
        builder.Property(x => x.BatteryPercent).HasColumnName("battery_percent");
        builder.Property(x => x.ConfirmedOwnershipVersion).HasColumnName("confirmed_ownership_version").IsRequired();
        builder.Property(x => x.AppliedSettingsVersion).HasColumnName("applied_settings_version").IsRequired();
        builder.Property(x => x.FirmwareVersion).HasColumnName("firmware_version").HasMaxLength(80);
        builder.Property(x => x.DeviceReportedAt).HasColumnName("device_reported_at").HasColumnType("timestamptz");
        builder.Property(x => x.ClockTrusted).HasColumnName("clock_trusted").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CurrentAlarmEventUid).HasColumnName("current_alarm_event_uid");

        builder.HasOne<Device>()
            .WithOne()
            .HasForeignKey<DeviceState>(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_device_states_sequence_nonnegative", "state_sequence >= 0");
            t.HasCheckConstraint("ck_device_states_confirmed_ownership_nonnegative", "confirmed_ownership_version >= 0");
            t.HasCheckConstraint("ck_device_states_applied_settings_nonnegative", "applied_settings_version >= 0");
            t.HasCheckConstraint("ck_device_states_battery_range", "battery_percent IS NULL OR (battery_percent BETWEEN 0 AND 100)");
            t.HasCheckConstraint("ck_device_states_arm", $"arm_state IN ({DbStatusCodes.Arm.SqlInList})");
            t.HasCheckConstraint("ck_device_states_siren", $"siren_state IN ({DbStatusCodes.Siren.SqlInList})");
            t.HasCheckConstraint("ck_device_states_health", $"health_state IN ({DbStatusCodes.Diagnostic.SqlInList})");
            t.HasCheckConstraint(
                "ck_device_states_power_source",
                $"power_source IS NULL OR power_source IN ({DbStatusCodes.PowerSource.SqlInList})");
        });
    }
}
