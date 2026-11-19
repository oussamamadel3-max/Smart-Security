#include "unity.h"
#include "connection_lifecycle.h"

void setUp(void) {}
void tearDown(void) {}

void test_happy_path_to_connected(void)
{
    conn_lifecycle_t life;
    conn_lifecycle_init(&life, 1000, 8000);
    TEST_ASSERT_FALSE(conn_lifecycle_is_mqtt_ready(&life));
    conn_lifecycle_begin_wifi(&life);
    TEST_ASSERT_EQUAL(CONN_LIFECYCLE_WIFI_CONNECTING, life.state);
    conn_lifecycle_on_wifi_connected(&life);
    conn_lifecycle_begin_time_sync(&life);
    conn_lifecycle_on_time_sync_done(&life);
    conn_lifecycle_begin_mqtt(&life);
    conn_lifecycle_on_mqtt_connected(&life);
    TEST_ASSERT_TRUE(conn_lifecycle_is_mqtt_ready(&life));
    TEST_ASSERT_EQUAL_UINT32(0, life.reconnect_attempt);
}

void test_backoff_caps_and_increments(void)
{
    conn_lifecycle_t life;
    conn_lifecycle_init(&life, 1000, 8000);
    TEST_ASSERT_EQUAL_UINT32(1000, conn_lifecycle_next_backoff_ms(&life));
    conn_lifecycle_on_transport_lost(&life);
    TEST_ASSERT_EQUAL(CONN_LIFECYCLE_RECONNECTING, life.state);
    TEST_ASSERT_EQUAL_UINT32(1, life.reconnect_attempt);
    TEST_ASSERT_EQUAL_UINT32(1000, conn_lifecycle_next_backoff_ms(&life));
    conn_lifecycle_on_transport_lost(&life);
    TEST_ASSERT_EQUAL_UINT32(2000, conn_lifecycle_next_backoff_ms(&life));
    conn_lifecycle_on_transport_lost(&life);
    TEST_ASSERT_EQUAL_UINT32(4000, conn_lifecycle_next_backoff_ms(&life));
    conn_lifecycle_on_transport_lost(&life);
    TEST_ASSERT_EQUAL_UINT32(8000, conn_lifecycle_next_backoff_ms(&life));
    conn_lifecycle_on_transport_lost(&life);
    TEST_ASSERT_EQUAL_UINT32(8000, conn_lifecycle_next_backoff_ms(&life)); /* capped */
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_happy_path_to_connected);
    RUN_TEST(test_backoff_caps_and_increments);
    return UNITY_END();
}
