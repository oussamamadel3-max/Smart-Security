using Domain.Entities.Auth;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notifications;

public sealed class AdminAlertConfiguration : IEntityTypeConfiguration<AdminAlert>
{
    public void Configure(EntityTypeBuilder<AdminAlert> builder)
    {
        builder.ToTable("admin_alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_admin_alerts_title_not_blank", "length(btrim(title)) > 0");
            t.HasCheckConstraint("ck_admin_alerts_body_not_blank", "length(btrim(body)) > 0");
        });
    }
}
