using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_attempt_count_nonnegative", "attempt_count >= 0");
                    table.CheckConstraint("ck_outbox_messages_payload_object", "jsonb_typeof(payload) = 'object'");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_user_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_deleted_implies_inactive", "deleted_at IS NULL OR is_active = false");
                    table.CheckConstraint("ck_users_username_not_blank", "length(btrim(username)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admin_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_alerts", x => x.id);
                    table.CheckConstraint("ck_admin_alerts_body_not_blank", "length(btrim(body)) > 0");
                    table.CheckConstraint("ck_admin_alerts_title_not_blank", "length(btrim(title)) > 0");
                    table.ForeignKey(
                        name: "FK_admin_alerts_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_admin_alerts_users_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hardware_uid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    current_ownership_version = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                    table.CheckConstraint("ck_devices_hardware_uid_not_blank", "length(btrim(hardware_uid)) > 0");
                    table.CheckConstraint("ck_devices_lifecycle_status", "lifecycle_status IN ('REGISTERED','ACTIVE','RETIRED')");
                    table.CheckConstraint("ck_devices_ownership_version_nonnegative", "current_ownership_version >= 0");
                    table.CheckConstraint("ck_devices_retired_at_matches_status", "(lifecycle_status = 'RETIRED' AND retired_at IS NOT NULL) OR (lifecycle_status <> 'RETIRED' AND retired_at IS NULL)");
                    table.ForeignKey(
                        name: "FK_devices_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "push_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    token_fingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    platform = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_tokens", x => x.id);
                    table.CheckConstraint("ck_push_tokens_platform", "platform IN ('ANDROID','IOS','WEB')");
                    table.ForeignKey(
                        name: "FK_push_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.CheckConstraint("ck_audit_logs_result", "result IN ('SUCCESS','REJECTED','FAILED')");
                    table.ForeignKey(
                        name: "FK_audit_logs_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verifier = table.Column<string>(type: "text", nullable: true),
                    certificate_fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    secret_ref = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_credentials", x => x.id);
                    table.CheckConstraint("ck_device_credentials_auth_type", "auth_type IN ('PASSWORD_HASH','CERTIFICATE','SECRET_REF')");
                    table.CheckConstraint("ck_device_credentials_fields_match_type", "(auth_type = 'PASSWORD_HASH' AND verifier IS NOT NULL AND certificate_fingerprint IS NULL AND secret_ref IS NULL)\r\nOR (auth_type = 'CERTIFICATE' AND certificate_fingerprint IS NOT NULL AND verifier IS NULL AND secret_ref IS NULL)\r\nOR (auth_type = 'SECRET_REF' AND secret_ref IS NOT NULL AND verifier IS NULL AND certificate_fingerprint IS NULL)");
                    table.ForeignKey(
                        name: "FK_device_credentials_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_ownerships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ended_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_ownerships", x => x.id);
                    table.UniqueConstraint("AK_device_ownerships_device_id_id", x => new { x.device_id, x.id });
                    table.CheckConstraint("ck_device_ownerships_active_state", "status <> 'ACTIVE' OR (activated_at IS NOT NULL AND ended_at IS NULL)");
                    table.CheckConstraint("ck_device_ownerships_closed_have_ended_at", "status NOT IN ('ENDED','CANCELLED') OR ended_at IS NOT NULL");
                    table.CheckConstraint("ck_device_ownerships_status", "status IN ('PENDING','ACTIVE','ENDED','CANCELLED')");
                    table.CheckConstraint("ck_device_ownerships_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_device_ownerships_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_ownerships_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_ownerships_users_ended_by_user_id",
                        column: x => x.ended_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_ownerships_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_sensors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sensor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    diagnostic_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    diagnostic_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    last_signal_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_sensors", x => x.id);
                    table.UniqueConstraint("AK_device_sensors_device_id_id", x => new { x.device_id, x.id });
                    table.CheckConstraint("ck_device_sensors_diagnostic", "diagnostic_status IN ('HEALTHY','FAULT','UNKNOWN')");
                    table.CheckConstraint("ck_device_sensors_type", "sensor_type IN ('VIBRATION','MOTION','OTHER')");
                    table.ForeignKey(
                        name: "FK_device_sensors_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_settings",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    alarm_rule = table.Column<string>(type: "jsonb", nullable: false),
                    test_duration_ms = table.Column<int>(type: "integer", nullable: false),
                    heartbeat_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    command_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    event_buffer_capacity = table.Column<int>(type: "integer", nullable: false),
                    command_ttl_seconds = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_settings", x => x.device_id);
                    table.CheckConstraint("ck_device_settings_alarm_rule_object", "jsonb_typeof(alarm_rule) = 'object'");
                    table.CheckConstraint("ck_device_settings_buffer_capacity_positive", "event_buffer_capacity > 0");
                    table.CheckConstraint("ck_device_settings_command_timeout_positive", "command_timeout_seconds > 0");
                    table.CheckConstraint("ck_device_settings_command_ttl_positive", "command_ttl_seconds > 0");
                    table.CheckConstraint("ck_device_settings_heartbeat_positive", "heartbeat_timeout_seconds > 0");
                    table.CheckConstraint("ck_device_settings_test_duration_positive", "test_duration_ms > 0");
                    table.CheckConstraint("ck_device_settings_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_device_settings_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_settings_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_states",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_state_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    state_sequence = table.Column<long>(type: "bigint", nullable: false),
                    arm_state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    siren_state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    health_state = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    health_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    power_source = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    battery_percent = table.Column<short>(type: "smallint", nullable: true),
                    confirmed_ownership_version = table.Column<long>(type: "bigint", nullable: false),
                    applied_settings_version = table.Column<long>(type: "bigint", nullable: false),
                    firmware_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    device_reported_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    clock_trusted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    current_alarm_event_uid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_states", x => x.device_id);
                    table.CheckConstraint("ck_device_states_applied_settings_nonnegative", "applied_settings_version >= 0");
                    table.CheckConstraint("ck_device_states_arm", "arm_state IN ('ARMED','DISARMED','UNKNOWN')");
                    table.CheckConstraint("ck_device_states_battery_range", "battery_percent IS NULL OR (battery_percent BETWEEN 0 AND 100)");
                    table.CheckConstraint("ck_device_states_confirmed_ownership_nonnegative", "confirmed_ownership_version >= 0");
                    table.CheckConstraint("ck_device_states_health", "health_state IN ('HEALTHY','FAULT','UNKNOWN')");
                    table.CheckConstraint("ck_device_states_power_source", "power_source IS NULL OR power_source IN ('MAINS','BATTERY','UNKNOWN')");
                    table.CheckConstraint("ck_device_states_sequence_nonnegative", "state_sequence >= 0");
                    table.CheckConstraint("ck_device_states_siren", "siren_state IN ('OFF','ALARM','TEST','UNKNOWN')");
                    table.ForeignKey(
                        name: "FK_device_states_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_loss_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_uid = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    lost_count = table.Column<int>(type: "integer", nullable: true),
                    device_occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ownership_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_loss_reports", x => x.id);
                    table.CheckConstraint("ck_event_loss_reports_lost_count", "lost_count IS NULL OR lost_count >= 0");
                    table.CheckConstraint("ck_event_loss_reports_ownership_version_nonnegative", "ownership_version >= 0");
                    table.ForeignKey(
                        name: "FK_event_loss_reports_device_ownerships_device_id_ownership_id",
                        columns: x => new { x.device_id, x.ownership_id },
                        principalTable: "device_ownerships",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_event_loss_reports_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alarm_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_uid = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ownership_version = table.Column<long>(type: "bigint", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sensor_key_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    occurred_device_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ended_device_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ended_received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    muted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    clock_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    signal_summary = table.Column<string>(type: "jsonb", nullable: true),
                    was_buffered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarm_events", x => x.id);
                    table.UniqueConstraint("AK_alarm_events_device_id_id", x => new { x.device_id, x.id });
                    table.CheckConstraint("ck_alarm_events_ownership_version_nonnegative", "ownership_version >= 0");
                    table.CheckConstraint("ck_alarm_events_status", "status IN ('ACTIVE','ENDED')");
                    table.ForeignKey(
                        name: "FK_alarm_events_device_ownerships_device_id_ownership_id",
                        columns: x => new { x.device_id, x.ownership_id },
                        principalTable: "device_ownerships",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alarm_events_device_sensors_device_id_sensor_id",
                        columns: x => new { x.device_id, x.sensor_id },
                        principalTable: "device_sensors",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alarm_events_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_faults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fault_uid = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fault_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    device_occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ownership_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_faults", x => x.id);
                    table.CheckConstraint("ck_device_faults_ownership_version_nonnegative", "ownership_version >= 0");
                    table.CheckConstraint("ck_device_faults_status", "status IN ('ACTIVE','RESOLVED')");
                    table.ForeignKey(
                        name: "FK_device_faults_device_ownerships_device_id_ownership_id",
                        columns: x => new { x.device_id, x.ownership_id },
                        principalTable: "device_ownerships",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_faults_device_sensors_device_id_sensor_id",
                        columns: x => new { x.device_id, x.sensor_id },
                        principalTable: "device_sensors",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_faults_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_commands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_version = table.Column<long>(type: "bigint", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_sequence = table.Column<long>(type: "bigint", nullable: false),
                    command_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    alarm_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reject_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: true),
                    result_state_sequence = table.Column<long>(type: "bigint", nullable: true),
                    target_alarm_event_uid = table.Column<Guid>(type: "uuid", nullable: true),
                    observed_state_sequence = table.Column<long>(type: "bigint", nullable: false),
                    reconciled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_commands", x => x.id);
                    table.CheckConstraint("ck_device_commands_expires_after_created", "expires_at > created_at");
                    table.CheckConstraint("ck_device_commands_mute_requires_target_uid", "command_type <> 'MUTE_ALARM' OR target_alarm_event_uid IS NOT NULL");
                    table.CheckConstraint("ck_device_commands_status", "status IN ('PENDING','EXECUTED','REJECTED','TIMED_OUT','CANCELLED')");
                    table.CheckConstraint("ck_device_commands_type", "command_type IN ('ARM','DISARM','MUTE_ALARM','TEST_SIREN','STOP_TEST')");
                    table.ForeignKey(
                        name: "FK_device_commands_alarm_events_device_id_alarm_event_id",
                        columns: x => new { x.device_id, x.alarm_event_id },
                        principalTable: "alarm_events",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_commands_device_ownerships_device_id_ownership_id",
                        columns: x => new { x.device_id, x.ownership_id },
                        principalTable: "device_ownerships",
                        principalColumns: new[] { "device_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_commands_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_device_commands_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    alarm_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    device_fault_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admin_alert_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.CheckConstraint("ck_notifications_single_source", "(\r\n  (notification_type = 'ALARM' AND alarm_event_id IS NOT NULL AND device_fault_id IS NULL AND admin_alert_id IS NULL)\r\n  OR (notification_type = 'FAULT' AND device_fault_id IS NOT NULL AND alarm_event_id IS NULL AND admin_alert_id IS NULL)\r\n  OR (notification_type = 'ADMIN_ALERT' AND admin_alert_id IS NOT NULL AND alarm_event_id IS NULL AND device_fault_id IS NULL)\r\n)");
                    table.CheckConstraint("ck_notifications_status", "status IN ('QUEUED','SENDING','ACCEPTED','FAILED','CANCELLED')");
                    table.CheckConstraint("ck_notifications_type", "notification_type IN ('ALARM','FAULT','ADMIN_ALERT')");
                    table.ForeignKey(
                        name: "FK_notifications_admin_alerts_admin_alert_id",
                        column: x => x.admin_alert_id,
                        principalTable: "admin_alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notifications_alarm_events_alarm_event_id",
                        column: x => x.alarm_event_id,
                        principalTable: "alarm_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notifications_device_faults_device_fault_id",
                        column: x => x.device_fault_id,
                        principalTable: "device_faults",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notifications_users_recipient_user_id",
                        column: x => x.recipient_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "command_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    command_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    to_status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    is_late_ack = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_status_history", x => x.id);
                    table.CheckConstraint("ck_command_status_history_from_status", "from_status IS NULL OR from_status IN ('PENDING','EXECUTED','REJECTED','TIMED_OUT','CANCELLED')");
                    table.CheckConstraint("ck_command_status_history_to_status", "to_status IN ('PENDING','EXECUTED','REJECTED','TIMED_OUT','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_command_status_history_device_commands_command_id",
                        column: x => x.command_id,
                        principalTable: "device_commands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_deliveries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    push_token_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_no = table.Column<short>(type: "smallint", nullable: false),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    provider_result = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_deliveries", x => x.id);
                    table.CheckConstraint("ck_notification_deliveries_attempt_positive", "attempt_no > 0");
                    table.CheckConstraint("ck_notification_deliveries_provider_result", "provider_result IN ('ACCEPTED','RETRYABLE','FAILED')");
                    table.ForeignKey(
                        name: "FK_notification_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_deliveries_push_tokens_push_token_id",
                        column: x => x.push_token_id,
                        principalTable: "push_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_alerts_recipient_user_id",
                table: "admin_alerts",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_alerts_sender_user_id",
                table: "admin_alerts",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_alarm_events_device_id_ownership_id",
                table: "alarm_events",
                columns: new[] { "device_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "IX_alarm_events_device_id_sensor_id",
                table: "alarm_events",
                columns: new[] { "device_id", "sensor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_alarm_events_device_received",
                table: "alarm_events",
                columns: new[] { "device_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_alarm_events_ownership_received",
                table: "alarm_events",
                columns: new[] { "ownership_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_alarm_event_uid",
                table: "alarm_events",
                columns: new[] { "device_id", "event_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_alarm_events_device_id_id",
                table: "alarm_events",
                columns: new[] { "device_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action_occurred",
                table: "audit_logs",
                columns: new[] { "action", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_occurred",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_device_occurred",
                table: "audit_logs",
                columns: new[] { "device_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_command_status_history_command_changed",
                table: "command_status_history",
                columns: new[] { "command_id", "changed_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_device_commands_device_created",
                table: "device_commands",
                columns: new[] { "device_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_device_id_alarm_event_id",
                table: "device_commands",
                columns: new[] { "device_id", "alarm_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_device_id_ownership_id",
                table: "device_commands",
                columns: new[] { "device_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "ix_device_commands_ownership_created",
                table: "device_commands",
                columns: new[] { "ownership_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_device_commands_pending_expires",
                table: "device_commands",
                columns: new[] { "status", "expires_at" },
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "ux_device_commands_device_sequence",
                table: "device_commands",
                columns: new[] { "device_id", "command_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_device_commands_idempotency",
                table: "device_commands",
                columns: new[] { "requested_by_user_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_device_pending_command",
                table: "device_commands",
                column: "device_id",
                unique: true,
                filter: "status = 'PENDING'");

            migrationBuilder.CreateIndex(
                name: "ix_device_credentials_device_id",
                table: "device_credentials",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_faults_device_id_ownership_id",
                table: "device_faults",
                columns: new[] { "device_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "IX_device_faults_device_id_sensor_id",
                table: "device_faults",
                columns: new[] { "device_id", "sensor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_device_faults_device_status_received",
                table: "device_faults",
                columns: new[] { "device_id", "status", "received_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ux_device_faults_fault_uid",
                table: "device_faults",
                columns: new[] { "device_id", "fault_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_ownerships_created_by_user_id",
                table: "device_ownerships",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_ownerships_ended_by_user_id",
                table: "device_ownerships",
                column: "ended_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_ownerships_owner_status",
                table: "device_ownerships",
                columns: new[] { "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_device_open_ownership",
                table: "device_ownerships",
                column: "device_id",
                unique: true,
                filter: "ended_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_device_ownerships_device_id_id",
                table: "device_ownerships",
                columns: new[] { "device_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_device_ownerships_device_version",
                table: "device_ownerships",
                columns: new[] { "device_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_device_sensors_device_id_id",
                table: "device_sensors",
                columns: new[] { "device_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_device_sensors_device_key",
                table: "device_sensors",
                columns: new[] { "device_id", "sensor_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_settings_updated_by_user_id",
                table: "device_settings",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_devices_created_by_user_id",
                table: "devices",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_lifecycle_status",
                table: "devices",
                column: "lifecycle_status");

            migrationBuilder.CreateIndex(
                name: "ux_devices_hardware_uid",
                table: "devices",
                column: "hardware_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_loss_reports_device_id_ownership_id",
                table: "event_loss_reports",
                columns: new[] { "device_id", "ownership_id" });

            migrationBuilder.CreateIndex(
                name: "ux_event_loss_reports_uid",
                table: "event_loss_reports",
                columns: new[] { "device_id", "report_uid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_push_token_id",
                table: "notification_deliveries",
                column: "push_token_id");

            migrationBuilder.CreateIndex(
                name: "ux_notification_deliveries_attempt",
                table: "notification_deliveries",
                columns: new[] { "notification_id", "push_token_id", "attempt_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_admin_alert_id",
                table: "notifications",
                column: "admin_alert_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_alarm_event_id",
                table: "notifications",
                column: "alarm_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_device_fault_id",
                table: "notifications",
                column: "device_fault_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_status_next_attempt",
                table: "notifications",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipient_admin_alert",
                table: "notifications",
                columns: new[] { "recipient_user_id", "admin_alert_id" },
                unique: true,
                filter: "admin_alert_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipient_alarm",
                table: "notifications",
                columns: new[] { "recipient_user_id", "alarm_event_id" },
                unique: true,
                filter: "alarm_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_notifications_recipient_fault",
                table: "notifications",
                columns: new[] { "recipient_user_id", "device_fault_id" },
                unique: true,
                filter: "device_fault_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                columns: new[] { "available_at", "created_at" },
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_outbox_messages_type_aggregate",
                table: "outbox_messages",
                columns: new[] { "message_type", "aggregate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_push_tokens_user_active",
                table: "push_tokens",
                column: "user_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_push_tokens_fingerprint",
                table: "push_tokens",
                column: "token_fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_claims_role_id",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_user_id",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_user_id",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by_user_id",
                table: "user_roles",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            // Fluent API لا يدعم فهارس التعبير (lower(...)) — مطلوب صراحة في الوثيقة §3.1.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_users_username_lower ON users (lower(username));
                CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email)) WHERE email IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS ux_users_email_lower;
                DROP INDEX IF EXISTS ux_users_username_lower;
                """);

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "command_status_history");

            migrationBuilder.DropTable(
                name: "device_credentials");

            migrationBuilder.DropTable(
                name: "device_settings");

            migrationBuilder.DropTable(
                name: "device_states");

            migrationBuilder.DropTable(
                name: "event_loss_reports");

            migrationBuilder.DropTable(
                name: "notification_deliveries");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "device_commands");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "push_tokens");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "admin_alerts");

            migrationBuilder.DropTable(
                name: "alarm_events");

            migrationBuilder.DropTable(
                name: "device_faults");

            migrationBuilder.DropTable(
                name: "device_ownerships");

            migrationBuilder.DropTable(
                name: "device_sensors");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
