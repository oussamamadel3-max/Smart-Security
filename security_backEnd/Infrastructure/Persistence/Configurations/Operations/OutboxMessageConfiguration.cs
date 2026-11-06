using Domain.Entities.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Operations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id").IsRequired();
        builder.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.AvailableAt).HasColumnName("available_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(x => x.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(80);

        builder.HasIndex(x => new { x.MessageType, x.AggregateId }).IsUnique().HasDatabaseName("ux_outbox_messages_type_aggregate");
        builder.HasIndex(x => new { x.AvailableAt, x.CreatedAt })
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_outbox_messages_attempt_count_nonnegative", "attempt_count >= 0");
            t.HasCheckConstraint("ck_outbox_messages_payload_object", "jsonb_typeof(payload) = 'object'");
        });
    }
}
