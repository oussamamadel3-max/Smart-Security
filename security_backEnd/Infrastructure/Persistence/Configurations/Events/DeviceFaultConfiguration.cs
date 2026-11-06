using Domain.Constants;
using Domain.Entities.Devices;
using Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Events;

public sealed class DeviceFaultConfiguration : IEntityTypeConfiguration<DeviceFault>
{
    public void Configure(EntityTypeBuilder<DeviceFault> builder)
    {
        builder.ToTable("device_faults");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.FaultUid).HasColumnName("fault_uid").IsRequired();
        builder.Property(x => x.OwnershipId).HasColumnName("ownership_id");
        builder.Property(x => x.SensorId).HasColumnName("sensor_id");
        builder.Property(x => x.FaultCode).HasColumnName("fault_code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.DeviceOccurredAt).HasColumnName("device_occurred_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        builder.Property(x => x.Details).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(x => x.OwnershipVersion).HasColumnName("ownership_version").IsRequired();

        builder.HasIndex(x => new { x.DeviceId, x.FaultUid }).IsUnique().HasDatabaseName("ux_device_faults_fault_uid");
        builder.HasIndex(x => new { x.DeviceId, x.Status, x.ReceivedAt }).IsDescending(false, false, true)
            .HasDatabaseName("ix_device_faults_device_status_received");

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
            t.HasCheckConstraint("ck_device_faults_status", $"status IN ({DbStatusCodes.Fault.SqlInList})");
            t.HasCheckConstraint("ck_device_faults_ownership_version_nonnegative", "ownership_version >= 0");
        });
    }
}
