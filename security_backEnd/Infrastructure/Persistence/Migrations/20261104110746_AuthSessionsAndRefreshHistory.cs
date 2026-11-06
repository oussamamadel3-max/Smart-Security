using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthSessionsAndRefreshHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    current_refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    absolute_expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.id);
                    table.CheckConstraint("ck_auth_sessions_absolute_after_created", "absolute_expires_at > created_at");
                    table.CheckConstraint("ck_auth_sessions_refresh_channel", "refresh_channel IN ('COOKIE','BODY')");
                    table.ForeignKey(
                        name: "FK_auth_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auth_refresh_token_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_refresh_token_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_auth_refresh_token_history_auth_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "auth_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_auth_refresh_history_family_consumed",
                table: "auth_refresh_token_history",
                columns: new[] { "family_id", "consumed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_refresh_token_history_session_id",
                table: "auth_refresh_token_history",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ux_auth_refresh_history_token_hash",
                table: "auth_refresh_token_history",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_family_id",
                table: "auth_sessions",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_auth_sessions_user_revoked",
                table: "auth_sessions",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ux_auth_sessions_current_refresh_hash",
                table: "auth_sessions",
                column: "current_refresh_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_refresh_token_history");

            migrationBuilder.DropTable(
                name: "auth_sessions");
        }
    }
}
