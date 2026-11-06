using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UsersFullNameCheckAndSoftDeleteUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_full_name_not_blank",
                table: "users",
                sql: "length(btrim(full_name)) > 0");

            // فهارس التعبير خارج نموذج EF — يُعاد إنشاؤها بفلتر الأرشفة المنطقية.
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_users_username_lower;
                DROP INDEX IF EXISTS ux_users_email_lower;
                CREATE UNIQUE INDEX ux_users_username_lower ON users (lower(username)) WHERE deleted_at IS NULL;
                CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email)) WHERE email IS NOT NULL AND deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_users_email_lower;
                DROP INDEX IF EXISTS ux_users_username_lower;
                CREATE UNIQUE INDEX ux_users_username_lower ON users (lower(username));
                CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email)) WHERE email IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_full_name_not_blank",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_user_name",
                unique: true);
        }
    }
}
