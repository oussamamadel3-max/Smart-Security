using Domain.Constants;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

public sealed class DeviceCredentialConfiguration : IEntityTypeConfiguration<DeviceCredential>
{
    public void Configure(EntityTypeBuilder<DeviceCredential> builder)
    {
        builder.ToTable("device_credentials");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(x => x.AuthType).HasColumnName("auth_type").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Verifier).HasColumnName("verifier");
        builder.Property(x => x.CertificateFingerprint).HasColumnName("certificate_fingerprint").HasMaxLength(128);
        builder.Property(x => x.SecretRef).HasColumnName("secret_ref");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasIndex(x => x.DeviceId).HasDatabaseName("ix_device_credentials_device_id");

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_device_credentials_auth_type",
                $"auth_type IN ({DbStatusCodes.AuthType.SqlInList})");
            t.HasCheckConstraint(
                "ck_device_credentials_fields_match_type",
                """
                (auth_type = 'PASSWORD_HASH' AND verifier IS NOT NULL AND certificate_fingerprint IS NULL AND secret_ref IS NULL)
                OR (auth_type = 'CERTIFICATE' AND certificate_fingerprint IS NOT NULL AND verifier IS NULL AND secret_ref IS NULL)
                OR (auth_type = 'SECRET_REF' AND secret_ref IS NOT NULL AND verifier IS NULL AND certificate_fingerprint IS NULL)
                """);
        });
    }
}
