#include "unity.h"
#include "heartbeat_json.h"
#include "alarm_event_json.h"
#include "cJSON.h"
#include <stdlib.h>

void setUp(void) {}
void tearDown(void) {}

void test_heartbeat_json_fields(void)
{
    char *json = heartbeat_to_json("11111111-1111-4111-8111-111111111111",
                                   "55555555-5555-4555-8555-555555555556",
                                   "boot-example", false);
    TEST_ASSERT_NOT_NULL(json);
    cJSON *root = cJSON_Parse(json);
    TEST_ASSERT_EQUAL_STRING("HEARTBEAT", cJSON_GetObjectItem(root, "messageType")->valuestring);
    TEST_ASSERT_FALSE(cJSON_IsTrue(cJSON_GetObjectItem(root, "clockTrusted")));
    cJSON_Delete(root);
    free(json);
}

void test_alarm_event_json_fields(void)
{
    uuid128_t uid;
    TEST_ASSERT_TRUE(uuid128_from_string("33333333-3333-4333-8333-333333333333", &uid));
    alarm_event_wire_t ev = {
        .event_uid = uid,
        .ownership_version = 2,
        .sensor_key = "vibration-1",
        .was_buffered = true,
        .clock_trusted = false,
        .alarm_phase_ended = false,
        .include_alarm_phase = false,
    };
    char *json = alarm_event_to_json(&ev,
                                     "11111111-1111-4111-8111-111111111111",
                                     "66666666-6666-4666-8666-666666666666",
                                     "boot-example");
    TEST_ASSERT_NOT_NULL(json);
    cJSON *root = cJSON_Parse(json);
    TEST_ASSERT_EQUAL_STRING("ALARM_EVENT", cJSON_GetObjectItem(root, "messageType")->valuestring);
    TEST_ASSERT_EQUAL_STRING("vibration-1", cJSON_GetObjectItem(root, "sensorKey")->valuestring);
    TEST_ASSERT_NULL(cJSON_GetObjectItem(root, "alarmPhase"));
    cJSON_Delete(root);
    free(json);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_heartbeat_json_fields);
    RUN_TEST(test_alarm_event_json_fields);
    return UNITY_END();
}
