#include "unity.h"
#include "mqtt_topics.h"

void setUp(void) {}
void tearDown(void) {}

void test_topics_match_draft_table(void)
{
    const char *id = "11111111-1111-4111-8111-111111111111";
    char topic[MQTT_TOPIC_MAX_LEN];
    TEST_ASSERT_TRUE(mqtt_topic_state(id, topic, sizeof(topic)));
    TEST_ASSERT_EQUAL_STRING("devices/11111111-1111-4111-8111-111111111111/state", topic);
    TEST_ASSERT_TRUE(mqtt_topic_events(id, topic, sizeof(topic)));
    TEST_ASSERT_EQUAL_STRING("devices/11111111-1111-4111-8111-111111111111/events", topic);
    TEST_ASSERT_TRUE(mqtt_topic_acks(id, topic, sizeof(topic)));
    TEST_ASSERT_EQUAL_STRING("devices/11111111-1111-4111-8111-111111111111/acks", topic);
    TEST_ASSERT_TRUE(mqtt_topic_commands(id, topic, sizeof(topic)));
    TEST_ASSERT_EQUAL_STRING("devices/11111111-1111-4111-8111-111111111111/commands", topic);
}

void test_topics_reject_bad_device_id(void)
{
    char topic[MQTT_TOPIC_MAX_LEN];
    TEST_ASSERT_FALSE(mqtt_topic_state("short", topic, sizeof(topic)));
    TEST_ASSERT_FALSE(mqtt_topic_state(NULL, topic, sizeof(topic)));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_topics_match_draft_table);
    RUN_TEST(test_topics_reject_bad_device_id);
    return UNITY_END();
}
