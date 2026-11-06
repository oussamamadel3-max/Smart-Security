using Domain.Constants;
using Domain.Entities.Auth;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Notifications;

public sealed class PushTokenConfiguration : IEntityTypeConfiguration<PushToken>
{
    public void Configure(EntityTypeBuilder<PushToken> builder)
    {
        builder.ToTable("push_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.TokenCiphertext).HasColumnName("token_ciphertext").HasColumnType("bytea").IsRequired();
        builder.Property(x => x.TokenFingerprint).HasColumnName("token_fingerprint").HasColumnType("bytea").IsRequired();
        builder.Property(x => x.Platform).HasColumnName("platform").HasMaxLength(12).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnName("registered_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasIndex(x => x.TokenFingerprint).IsUnique().HasDatabaseName("ux_push_tokens_fingerprint");
        builder.HasIndex(x => x.UserId)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ix_push_tokens_user_active");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_push_tokens_platform",
                $"platform IN ({DbStatusCodes.PushPlatform.SqlInList})");
        });
    }
}
