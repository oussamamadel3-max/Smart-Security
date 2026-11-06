using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceOwnershipConfiguration : IEntityTypeConfiguration<DeviceOwnership>
{
    public void Configure(EntityTypeBuilder<DeviceOwnership> builder)
    {
        builder.ToTable("device_ownerships");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ActivatedAt).HasColumnName("activated_at").HasColumnType("timestamptz");
        builder.Property(x => x.EndedAt).HasColumnName("ended_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.EndedByUserId).HasColumnName("ended_by_user_id");

        // لدعم المفاتيح الخارجية المركبة من الأوامر/الأحداث.
        builder.HasIndex(x => new { x.DeviceId, x.Id }).IsUnique().HasDatabaseName("ux_device_ownerships_device_id_id");
        builder.HasIndex(x => new { x.DeviceId, x.Version }).IsUnique().HasDatabaseName("ux_device_ownerships_device_version");
        builder.HasIndex(x => x.DeviceId)
            .IsUnique()
            .HasFilter("ended_at IS NULL")
            .HasDatabaseName("ux_device_open_ownership");
        builder.HasIndex(x => new { x.OwnerUserId, x.Status }).HasDatabaseName("ix_device_ownerships_owner_status");

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.EndedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_device_ownerships_version_positive", "version > 0");
            t.HasCheckConstraint(
                "ck_device_ownerships_status",
                $"status IN ({DbStatusCodes.Ownership.SqlInList})");
            t.HasCheckConstraint(
                "ck_device_ownerships_active_state",
                "status <> 'ACTIVE' OR (activated_at IS NOT NULL AND ended_at IS NULL)");
            t.HasCheckConstraint(
                "ck_device_ownerships_closed_have_ended_at",
                "status NOT IN ('ENDED','CANCELLED') OR ended_at IS NOT NULL");
        });
    }
}
