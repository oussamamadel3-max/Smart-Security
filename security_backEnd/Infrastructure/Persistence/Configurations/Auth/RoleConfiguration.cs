using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Auth;

/// <summary>
/// تهيئة جدول roles — Name يُخزَّن كعمود code (OWNER/ADMIN).
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.NormalizedName).HasColumnName("normalized_name").HasMaxLength(40);
        builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        builder.Property(x => x.DisplayName).HasColumnName("name").HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_roles_code");
    }
}
