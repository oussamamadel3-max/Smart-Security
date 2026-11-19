#include "unity.h"
#include "inbound_message.h"

void setUp(void) {}
void tearDown(void) {}

void test_classify_execute_command(void)
{
    const char *json = "{\"messageType\":\"EXECUTE_COMMAND\",\"schemaVersion\":1}";
    TEST_ASSERT_EQUAL(INBOUND_MSG_EXECUTE_COMMAND, inbound_message_classify(json));
}

void test_classify_ownership_and_persisted(void)
{
    TEST_ASSERT_EQUAL(INBOUND_MSG_OWNERSHIP_SYNC,
                      inbound_message_classify("{\"messageType\":\"OWNERSHIP_SYNC\"}"));
    TEST_ASSERT_EQUAL(INBOUND_MSG_EVENT_PERSISTED,
                      inbound_message_classify("{\"messageType\":\"EVENT_PERSISTED\"}"));
}

void test_classify_unknown_and_settings_sync(void)
{
    TEST_ASSERT_EQUAL(INBOUND_MSG_UNKNOWN, inbound_message_classify(NULL));
    TEST_ASSERT_EQUAL(INBOUND_MSG_UNKNOWN,
                      inbound_message_classify("{\"messageType\":\"SETTINGS_SYNC\"}"));
    TEST_ASSERT_EQUAL(INBOUND_MSG_UNKNOWN, inbound_message_classify("{}"));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_classify_execute_command);
    RUN_TEST(test_classify_ownership_and_persisted);
    RUN_TEST(test_classify_unknown_and_settings_sync);
    return UNITY_END();
}
