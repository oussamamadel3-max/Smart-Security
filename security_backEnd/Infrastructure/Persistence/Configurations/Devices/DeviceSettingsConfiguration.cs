using Domain.Entities.Auth;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceSettingsConfiguration : IEntityTypeConfiguration<DeviceSettings>
{
    public void Configure(EntityTypeBuilder<DeviceSettings> builder)
    {
        builder.ToTable("device_settings");
        builder.HasKey(x => x.DeviceId);

        builder.Property(x => x.DeviceId).HasColumnName("device_id");
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.AlarmRule).HasColumnName("alarm_rule").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TestDurationMs).HasColumnName("test_duration_ms").IsRequired();
        builder.Property(x => x.HeartbeatTimeoutSeconds).HasColumnName("heartbeat_timeout_seconds").IsRequired();
        builder.Property(x => x.CommandTimeoutSeconds).HasColumnName("command_timeout_seconds").IsRequired();
        // بلا DEFAULT إنتاجي: القيم الرقمية لسعة الأحداث وTTL معلقة على الاختبار (§11).
        builder.Property(x => x.EventBufferCapacity).HasColumnName("event_buffer_capacity").IsRequired();
        builder.Property(x => x.CommandTtlSeconds).HasColumnName("command_ttl_seconds").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id").IsRequired();

        builder.HasOne<Device>()
            .WithOne()
            .HasForeignKey<DeviceSettings>(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_device_settings_version_positive", "version > 0");
            t.HasCheckConstraint("ck_device_settings_test_duration_positive", "test_duration_ms > 0");
            t.HasCheckConstraint("ck_device_settings_heartbeat_positive", "heartbeat_timeout_seconds > 0");
            t.HasCheckConstraint("ck_device_settings_command_timeout_positive", "command_timeout_seconds > 0");
            t.HasCheckConstraint("ck_device_settings_buffer_capacity_positive", "event_buffer_capacity > 0");
            t.HasCheckConstraint("ck_device_settings_command_ttl_positive", "command_ttl_seconds > 0");
            t.HasCheckConstraint("ck_device_settings_alarm_rule_object", "jsonb_typeof(alarm_rule) = 'object'");
        });
    }
}
