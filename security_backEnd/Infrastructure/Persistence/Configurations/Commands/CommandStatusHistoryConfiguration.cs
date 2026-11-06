using Domain.Constants;
using Domain.Entities.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Commands;

public sealed class CommandStatusHistoryConfiguration : IEntityTypeConfiguration<CommandStatusHistory>
{
    public void Configure(EntityTypeBuilder<CommandStatusHistory> builder)
    {
        builder.ToTable("command_status_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.CommandId).HasColumnName("command_id").IsRequired();
        builder.Property(x => x.FromStatus).HasColumnName("from_status").HasMaxLength(12);
        builder.Property(x => x.ToStatus).HasColumnName("to_status").HasMaxLength(12).IsRequired();
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(80);
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.IsLateAck).HasColumnName("is_late_ack").IsRequired();

        builder.HasIndex(x => new { x.CommandId, x.ChangedAt, x.Id }).HasDatabaseName("ix_command_status_history_command_changed");

        builder.HasOne<DeviceCommand>()
            .WithMany()
            .HasForeignKey(x => x.CommandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_command_status_history_to_status",
                $"to_status IN ({DbStatusCodes.CommandStatus.SqlInList})");
            t.HasCheckConstraint(
                "ck_command_status_history_from_status",
                $"from_status IS NULL OR from_status IN ({DbStatusCodes.CommandStatus.SqlInList})");
        });
    }
}
