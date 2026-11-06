using Domain.Constants;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Auth;

public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FamilyId).HasColumnName("family_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.RefreshChannel).HasColumnName("refresh_channel").HasMaxLength(10).IsRequired();
        builder.Property(x => x.CurrentRefreshTokenHash).HasColumnName("current_refresh_token_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.AbsoluteExpiresAt).HasColumnName("absolute_expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at").HasColumnType("timestamptz");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(40);

        builder.HasIndex(x => x.CurrentRefreshTokenHash)
            .IsUnique()
            .HasDatabaseName("ux_auth_sessions_current_refresh_hash");
        builder.HasIndex(x => x.FamilyId).HasDatabaseName("ix_auth_sessions_family_id");
        builder.HasIndex(x => new { x.UserId, x.RevokedAt }).HasDatabaseName("ix_auth_sessions_user_revoked");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.TokenHistory)
            .WithOne()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_auth_sessions_refresh_channel",
                $"refresh_channel IN ({AuthStatusCodes.RefreshChannel.SqlInList})");
            t.HasCheckConstraint(
                "ck_auth_sessions_absolute_after_created",
                "absolute_expires_at > created_at");
        });
    }
}
