#include "unity.h"
#include "execute_command_wire.h"
#include "iso8601_time.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* Reads one of the real draft-contract fixture files shipped in
 * docs/contracts/mqtt/v1/samples/ — these tests parse the contract's own
 * examples, not ad hoc strings invented for this test file. Path is
 * relative to this binary's cwd, which build_and_run.sh always runs from
 * security_esp/firmware/tests/host. */
static char *read_fixture(const char *relative_path)
{
    char full_path[512];
    snprintf(full_path, sizeof(full_path), "../../../../docs/contracts/mqtt/v1/samples/%s", relative_path);
    FILE *f = fopen(full_path, "rb");
    if (f == NULL) {
        return NULL;
    }
    fseek(f, 0, SEEK_END);
    long size = ftell(f);
    fseek(f, 0, SEEK_SET);
    char *buf = (char *)malloc((size_t)size + 1);
    fread(buf, 1, (size_t)size, f);
    buf[size] = '\0';
    fclose(f);
    return buf;
}

void setUp(void) {}
void tearDown(void) {}

static void test_parses_real_accept_fixture_arm(void)
{
    char *json = read_fixture("accept/execute-command.json");
    TEST_ASSERT_NOT_NULL_MESSAGE(json, "fixture file not found — check relative path");

    execute_command_wire_t wire;
    char err[128] = {0};
    TEST_ASSERT_TRUE_MESSAGE(execute_command_parse(json, &wire, err, sizeof(err)), err);
    TEST_ASSERT_EQUAL_STRING("11111111-1111-4111-8111-111111111111", wire.device_id);
    TEST_ASSERT_EQUAL_STRING("22222222-2222-4222-8222-222222222222", wire.command_id);
    TEST_ASSERT_EQUAL_UINT64(7, wire.command_sequence);
    TEST_ASSERT_EQUAL_UINT64(2, wire.ownership_version);
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_TYPE_ARM, wire.type);
    TEST_ASSERT_FALSE(wire.has_target_event);

    int64_t expected_ms;
    iso8601_utc_to_epoch_ms("2026-10-25T12:00:00Z", &expected_ms);
    TEST_ASSERT_EQUAL_INT64(expected_ms, wire.expires_at_epoch_ms);

    free(json);
}

static void test_parses_real_accept_fixture_mute_with_target(void)
{
    char *json = read_fixture("accept/execute-command--mute.json");
    TEST_ASSERT_NOT_NULL_MESSAGE(json, "fixture file not found — check relative path");

    execute_command_wire_t wire;
    char err[128] = {0};
    TEST_ASSERT_TRUE_MESSAGE(execute_command_parse(json, &wire, err, sizeof(err)), err);
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_TYPE_MUTE_ALARM, wire.type);
    TEST_ASSERT_TRUE(wire.has_target_event);
    char uid_str[37];
    uuid128_to_string(&wire.target_alarm_event_uid, uid_str);
    TEST_ASSERT_EQUAL_STRING("33333333-3333-4333-8333-333333333333", uid_str);

    free(json);
}

/* The contract's own reject fixture: MUTE_ALARM without targetAlarmEventUid. */
static void test_rejects_real_fixture_mute_missing_target(void)
{
    char *json = read_fixture("reject/execute-command--mute-missing-target.json");
    TEST_ASSERT_NOT_NULL_MESSAGE(json, "fixture file not found — check relative path");

    execute_command_wire_t wire;
    char err[128] = {0};
    TEST_ASSERT_FALSE(execute_command_parse(json, &wire, err, sizeof(err)));

    free(json);
}

static void test_rejects_real_fixture_bad_device_id(void)
{
    char *json = read_fixture("reject/execute-command--bad-device-id.json");
    TEST_ASSERT_NOT_NULL_MESSAGE(json, "fixture file not found — check relative path");

    execute_command_wire_t wire;
    char err[128] = {0};
    TEST_ASSERT_FALSE(execute_command_parse(json, &wire, err, sizeof(err)));

    free(json);
}

static void test_rejects_real_fixture_schema_version_2(void)
{
    char *json = read_fixture("reject/execute-command--schema-version-2.json");
    TEST_ASSERT_NOT_NULL_MESSAGE(json, "fixture file not found — check relative path");

    execute_command_wire_t wire;
    char err[128] = {0};
    TEST_ASSERT_FALSE(execute_command_parse(json, &wire, err, sizeof(err)));

    free(json);
}

/* Resolved (was "does_not_bridge"): target_event_uid is now a real bridge,
 * since the backend's authoritative schema already commits to a UUID for
 * event identity — see esp_core_types.h / uuid128.h. */
static void test_wire_to_core_bridges_target_event_uid(void)
{
    execute_command_wire_t wire;
    memset(&wire, 0, sizeof(wire));
    strcpy(wire.command_id, "22222222-2222-4222-8222-222222222222");
    wire.command_sequence = 5;
    wire.ownership_version = 2;
    wire.type = REMOTE_COMMAND_TYPE_MUTE_ALARM;
    wire.has_target_event = true;
    TEST_ASSERT_TRUE(uuid128_from_string("33333333-3333-4333-8333-333333333333", &wire.target_alarm_event_uid));

    execute_command_t core_cmd;
    execute_command_wire_to_core(&wire, &core_cmd);

    TEST_ASSERT_EQUAL_STRING("22222222-2222-4222-8222-222222222222", core_cmd.command_id);
    TEST_ASSERT_EQUAL_UINT64(5, core_cmd.command_sequence);
    TEST_ASSERT_TRUE(core_cmd.has_target_event);
    TEST_ASSERT_TRUE(uuid128_equal(&wire.target_alarm_event_uid, &core_cmd.target_event_uid));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_parses_real_accept_fixture_arm);
    RUN_TEST(test_parses_real_accept_fixture_mute_with_target);
    RUN_TEST(test_rejects_real_fixture_mute_missing_target);
    RUN_TEST(test_rejects_real_fixture_bad_device_id);
    RUN_TEST(test_rejects_real_fixture_schema_version_2);
    RUN_TEST(test_wire_to_core_bridges_target_event_uid);
    return UNITY_END();
}
