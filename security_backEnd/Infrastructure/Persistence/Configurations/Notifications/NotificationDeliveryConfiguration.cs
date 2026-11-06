using Domain.Constants;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.NotificationId).HasColumnName("notification_id").IsRequired();
        builder.Property(x => x.PushTokenId).HasColumnName("push_token_id").IsRequired();
        builder.Property(x => x.AttemptNo).HasColumnName("attempt_no").IsRequired();
        builder.Property(x => x.AttemptedAt).HasColumnName("attempted_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.ProviderResult).HasColumnName("provider_result").HasMaxLength(12).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(255);
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(80);

        builder.HasIndex(x => new { x.NotificationId, x.PushTokenId, x.AttemptNo })
            .IsUnique()
            .HasDatabaseName("ux_notification_deliveries_attempt");

        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PushToken>()
            .WithMany()
            .HasForeignKey(x => x.PushTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_notification_deliveries_attempt_positive", "attempt_no > 0");
            t.HasCheckConstraint(
                "ck_notification_deliveries_provider_result",
                $"provider_result IN ({DbStatusCodes.ProviderResult.SqlInList})");
        });
    }
}
