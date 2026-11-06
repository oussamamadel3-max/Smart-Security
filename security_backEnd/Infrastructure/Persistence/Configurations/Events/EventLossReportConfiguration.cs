using Domain.Entities.Devices;
using Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Events;

public sealed class EventLossReportConfiguration : IEntityTypeConfiguration<EventLossReport>
{
    public void Configure(EntityTypeBuilder<EventLossReport> builder)
    {
        builder.ToTable("event_loss_reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.ReportUid).HasColumnName("report_uid").IsRequired();
        builder.Property(x => x.OwnershipId).HasColumnName("ownership_id");
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(24).IsRequired();
        builder.Property(x => x.LostCount).HasColumnName("lost_count");
        builder.Property(x => x.DeviceOccurredAt).HasColumnName("device_occurred_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.OwnershipVersion).HasColumnName("ownership_version").IsRequired();

        builder.HasIndex(x => new { x.DeviceId, x.ReportUid }).IsUnique().HasDatabaseName("ux_event_loss_reports_uid");

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

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_event_loss_reports_lost_count", "lost_count IS NULL OR lost_count >= 0");
            t.HasCheckConstraint("ck_event_loss_reports_ownership_version_nonnegative", "ownership_version >= 0");
        });
    }
}
