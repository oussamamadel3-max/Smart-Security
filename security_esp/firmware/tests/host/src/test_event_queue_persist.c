#include "unity.h"
#include "event_queue_persist.h"
#include "fake_storage.h"
#include "uuid128.h"

static fake_storage_ctx_t fake;

void setUp(void)
{
    fake_storage_reset(&fake);
}

void tearDown(void) {}

static uuid128_t uid_from(const char *s)
{
    uuid128_t u;
    TEST_ASSERT_TRUE(uuid128_from_string(s, &u));
    return u;
}

void test_persist_roundtrip(void)
{
    event_queue_t q;
    TEST_ASSERT_TRUE(event_queue_init(&q, 4));
    uuid128_t u = uid_from("33333333-3333-4333-8333-333333333333");
    TEST_ASSERT_TRUE(event_queue_enqueue(&q, u, false, 1000, true));
    storage_backend_t be = fake_storage_backend(&fake);
    TEST_ASSERT_TRUE(event_queue_persist_save(&be, &q));

    event_queue_t loaded;
    TEST_ASSERT_EQUAL(EVENT_QUEUE_PERSIST_OK, event_queue_persist_load(&be, &loaded));
    TEST_ASSERT_EQUAL_UINT16(1, event_queue_count(&loaded));
    event_record_t rec;
    TEST_ASSERT_TRUE(event_queue_peek_oldest(&loaded, &rec));
    TEST_ASSERT_TRUE(uuid128_equal(&u, &rec.event_uid));
    TEST_ASSERT_TRUE(rec.was_buffered);
}

void test_persist_not_found(void)
{
    storage_backend_t be = fake_storage_backend(&fake);
    event_queue_t q;
    TEST_ASSERT_EQUAL(EVENT_QUEUE_PERSIST_NOT_FOUND, event_queue_persist_load(&be, &q));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_persist_roundtrip);
    RUN_TEST(test_persist_not_found);
    return UNITY_END();
}
