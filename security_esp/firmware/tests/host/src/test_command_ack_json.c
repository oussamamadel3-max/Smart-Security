#include "unity.h"
#include "command_ack_json.h"
#include "cJSON.h"
#include <string.h>
#include <stdlib.h>

static protection_core_t core;
static ownership_state_t ownership;

static void fake_uuid_source(void *ctx, uint8_t *out, size_t len)
{
    (void)ctx;
    for (size_t i = 0; i < len; i++) {
        out[i] = (uint8_t)(i + 3);
    }
}

void setUp(void)
{
    alarm_detector_config_t cfg = { .pulses_to_qualify = 2, .window_ms = 1000, .cooldown_ms = 3000 };
    protection_core_init(&core, &cfg, 30000, fake_uuid_source, NULL);
    ownership_state_init(&ownership);
}
void tearDown(void) {}

/* Field-by-field comparison against the contract's own accept fixture
 * (docs/contracts/mqtt/v1/samples/accept/command-ack.json), not a value
 * invented for this test. */
static void test_matches_real_accept_fixture_fields(void)
{
    protection_core_arm(&core);
    ownership_state_apply_sync(&ownership, 2, true);
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_UNKNOWN, &ownership, true, 10, true, NULL);

    char *json = command_ack_to_json(&ack, "11111111-1111-4111-8111-111111111111",
                                     "44444444-4444-4444-8444-444444444444",
                                     "boot-example", "22222222-2222-4222-8222-222222222222");
    TEST_ASSERT_NOT_NULL(json);

    cJSON *root = cJSON_Parse(json);
    TEST_ASSERT_NOT_NULL(root);
    TEST_ASSERT_EQUAL(1, (int)cJSON_GetObjectItem(root, "schemaVersion")->valuedouble);
    TEST_ASSERT_EQUAL_STRING("11111111-1111-4111-8111-111111111111", cJSON_GetObjectItem(root, "deviceId")->valuestring);
    TEST_ASSERT_EQUAL_STRING("COMMAND_ACK", cJSON_GetObjectItem(root, "messageType")->valuestring);
    TEST_ASSERT_EQUAL_STRING("boot-example", cJSON_GetObjectItem(root, "bootId")->valuestring);
    TEST_ASSERT_EQUAL_STRING("22222222-2222-4222-8222-222222222222", cJSON_GetObjectItem(root, "commandId")->valuestring);
    TEST_ASSERT_EQUAL(2, (int)cJSON_GetObjectItem(root, "ownershipVersion")->valuedouble);
    TEST_ASSERT_EQUAL_STRING("EXECUTED", cJSON_GetObjectItem(root, "status")->valuestring);
    TEST_ASSERT_EQUAL(10, (int)cJSON_GetObjectItem(root, "resultStateSequence")->valuedouble);
    TEST_ASSERT_EQUAL_STRING("ARMED", cJSON_GetObjectItem(root, "armState")->valuestring);
    TEST_ASSERT_EQUAL_STRING("OFF", cJSON_GetObjectItem(root, "sirenState")->valuestring);
    TEST_ASSERT_EQUAL_STRING("UNKNOWN", cJSON_GetObjectItem(root, "healthState")->valuestring);
    TEST_ASSERT_TRUE(cJSON_IsNull(cJSON_GetObjectItem(root, "currentAlarmEventUid")));

    cJSON_Delete(root);
    free(json);
}

/* Resolved gap: when an event is actually active, the real UUID must be
 * emitted as a string, not null (the earlier implementation always emitted
 * null because there was no real UUID to emit at all). */
static void test_active_alarm_emits_real_uuid_not_null(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_UNKNOWN, &ownership, true, 1, true, NULL);

    char *json = command_ack_to_json(&ack, "d", "m", "b", "c");
    cJSON *root = cJSON_Parse(json);
    cJSON *uid_item = cJSON_GetObjectItem(root, "currentAlarmEventUid");
    TEST_ASSERT_FALSE(cJSON_IsNull(uid_item));
    TEST_ASSERT_TRUE(cJSON_IsString(uid_item));
    TEST_ASSERT_EQUAL(36, strlen(uid_item->valuestring));

    cJSON_Delete(root);
    free(json);
}

static void test_rejected_ack_includes_reject_code(void)
{
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_UNKNOWN, &ownership, true, 1, false, "OWNERSHIP_MISMATCH");
    char *json = command_ack_to_json(&ack, "d", "m", "b", "c");
    TEST_ASSERT_NOT_NULL(json);

    cJSON *root = cJSON_Parse(json);
    TEST_ASSERT_EQUAL_STRING("REJECTED", cJSON_GetObjectItem(root, "status")->valuestring);
    TEST_ASSERT_EQUAL_STRING("OWNERSHIP_MISMATCH", cJSON_GetObjectItem(root, "rejectCode")->valuestring);

    cJSON_Delete(root);
    free(json);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_matches_real_accept_fixture_fields);
    RUN_TEST(test_active_alarm_emits_real_uuid_not_null);
    RUN_TEST(test_rejected_ack_includes_reject_code);
    return UNITY_END();
}
