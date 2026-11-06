using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.HardwareUid).HasColumnName("hardware_uid").HasMaxLength(128).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CurrentOwnershipVersion).HasColumnName("current_ownership_version").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RetiredAt).HasColumnName("retired_at").HasColumnType("timestamptz");

        builder.HasIndex(x => x.HardwareUid).IsUnique().HasDatabaseName("ux_devices_hardware_uid");
        builder.HasIndex(x => x.LifecycleStatus).HasDatabaseName("ix_devices_lifecycle_status");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_devices_lifecycle_status",
                $"lifecycle_status IN ({DbStatusCodes.DeviceLifecycle.SqlInList})");
            t.HasCheckConstraint("ck_devices_ownership_version_nonnegative", "current_ownership_version >= 0");
            t.HasCheckConstraint(
                "ck_devices_retired_at_matches_status",
                "(lifecycle_status = 'RETIRED' AND retired_at IS NOT NULL) OR (lifecycle_status <> 'RETIRED' AND retired_at IS NULL)");
            t.HasCheckConstraint("ck_devices_hardware_uid_not_blank", "length(btrim(hardware_uid)) > 0");
        });
    }
}
