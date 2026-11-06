using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Auth;

public sealed class AuthRefreshTokenHistoryConfiguration : IEntityTypeConfiguration<AuthRefreshTokenHistory>
{
    public void Configure(EntityTypeBuilder<AuthRefreshTokenHistory> builder)
    {
        builder.ToTable("auth_refresh_token_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(x => x.FamilyId).HasColumnName("family_id").IsRequired();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_auth_refresh_history_token_hash");
        builder.HasIndex(x => new { x.FamilyId, x.ConsumedAt })
            .HasDatabaseName("ix_auth_refresh_history_family_consumed");
    }
}
