#include "unity.h"
#include "state_json.h"
#include "cJSON.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

void setUp(void) {}
void tearDown(void) {}

void test_state_json_matches_sample_fields(void)
{
    state_snapshot_t snap = {
        .state_sequence = 10,
        .confirmed_ownership_version = 2,
        .arm_state = ARM_STATE_ARMED,
        .siren_state = SIREN_STATE_OFF,
        .health_state = HEALTH_STATE_UNKNOWN,
        .has_current_alarm_event_uid = false,
        .clock_trusted = false,
    };
    char *json = state_to_json(&snap,
                               "11111111-1111-4111-8111-111111111111",
                               "55555555-5555-4555-8555-555555555555",
                               "boot-example");
    TEST_ASSERT_NOT_NULL(json);
    cJSON *root = cJSON_Parse(json);
    TEST_ASSERT_NOT_NULL(root);
    TEST_ASSERT_EQUAL_STRING("STATE", cJSON_GetObjectItem(root, "messageType")->valuestring);
    TEST_ASSERT_EQUAL(10, (int)cJSON_GetObjectItem(root, "stateSequence")->valuedouble);
    TEST_ASSERT_EQUAL_STRING("ARMED", cJSON_GetObjectItem(root, "armState")->valuestring);
    TEST_ASSERT_TRUE(cJSON_IsNull(cJSON_GetObjectItem(root, "currentAlarmEventUid")));
    cJSON_Delete(root);
    free(json);

    /* Contract sample still present as an anchor (same relative path as other fixtures). */
    FILE *f = fopen("../../../../docs/contracts/mqtt/v1/samples/accept/state.json", "rb");
    TEST_ASSERT_NOT_NULL(f);
    fclose(f);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_state_json_matches_sample_fields);
    return UNITY_END();
}
