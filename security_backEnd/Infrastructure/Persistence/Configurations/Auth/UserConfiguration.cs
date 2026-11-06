using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Auth;

/// <summary>
/// تهيئة جدول users (Identity) وفق قاموس التصميم.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserName).HasColumnName("username").HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(120);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254);
        builder.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
        builder.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
        builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number");
        builder.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
        builder.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
        builder.Property(x => x.LockoutEnd).HasColumnName("lockout_end");
        builder.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
        builder.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");

        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // Identity يفرض تفرد NormalizedUserName دون فلتر؛ نقيّده بالحسابات غير المؤرشفة لإتاحة إعادة استخدام الاسم بعد soft-delete.
        builder.HasIndex(x => x.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("UserNameIndex")
            .HasFilter("deleted_at IS NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_users_username_not_blank", "length(btrim(username)) > 0");
            t.HasCheckConstraint("ck_users_full_name_not_blank", "length(btrim(full_name)) > 0");
            t.HasCheckConstraint("ck_users_deleted_implies_inactive", "deleted_at IS NULL OR is_active = false");
        });
    }
}
