using Domain.Constants;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceSensorConfiguration : IEntityTypeConfiguration<DeviceSensor>
{
    public void Configure(EntityTypeBuilder<DeviceSensor> builder)
    {
        builder.ToTable("device_sensors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.SensorKey).HasColumnName("sensor_key").HasMaxLength(64).IsRequired();
        builder.Property(x => x.SensorType).HasColumnName("sensor_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(x => x.DiagnosticStatus).HasColumnName("diagnostic_status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.DiagnosticCode).HasColumnName("diagnostic_code").HasMaxLength(80);
        builder.Property(x => x.LastSignalAt).HasColumnName("last_signal_at").HasColumnType("timestamptz");
        builder.Property(x => x.Configuration).HasColumnName("configuration").HasColumnType("jsonb");

        builder.HasIndex(x => new { x.DeviceId, x.SensorKey }).IsUnique().HasDatabaseName("ux_device_sensors_device_key");
        builder.HasIndex(x => new { x.DeviceId, x.Id }).IsUnique().HasDatabaseName("ux_device_sensors_device_id_id");

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_device_sensors_type",
                $"sensor_type IN ({DbStatusCodes.SensorType.SqlInList})");
            t.HasCheckConstraint(
                "ck_device_sensors_diagnostic",
                $"diagnostic_status IN ({DbStatusCodes.Diagnostic.SqlInList})");
        });
    }
}
