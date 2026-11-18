#include "unity.h"
#include "event_queue.h"

static event_queue_t q;

void setUp(void) { TEST_ASSERT_TRUE(event_queue_init(&q, 3)); }
void tearDown(void) {}

static void test_enqueue_and_count(void)
{
    TEST_ASSERT_TRUE(event_queue_enqueue(&q, 1, false, 100, false));
    TEST_ASSERT_EQUAL(1, event_queue_count(&q));
}

/* AC-T-09: a full queue drops the newest record and raises the loss flag; it
 * never evicts an existing, not-yet-acked record. */
static void test_full_queue_drops_new_and_flags_loss_without_evicting(void)
{
    TEST_ASSERT_TRUE(event_queue_enqueue(&q, 1, false, 100, false));
    TEST_ASSERT_TRUE(event_queue_enqueue(&q, 2, false, 200, false));
    TEST_ASSERT_TRUE(event_queue_enqueue(&q, 3, false, 300, false));
    TEST_ASSERT_TRUE(event_queue_is_full(&q));

    TEST_ASSERT_FALSE(event_queue_enqueue(&q, 4, false, 400, false));
    TEST_ASSERT_TRUE(event_queue_consume_loss_flag(&q));
    TEST_ASSERT_EQUAL(3, event_queue_count(&q));

    /* event 1 (the oldest) must still be there — not evicted to make room. */
    event_record_t out;
    TEST_ASSERT_TRUE(event_queue_peek_oldest(&q, &out));
    TEST_ASSERT_EQUAL(1, out.event_uid);
}

static void test_loss_flag_consumed_once(void)
{
    event_queue_enqueue(&q, 1, false, 0, false);
    event_queue_enqueue(&q, 2, false, 0, false);
    event_queue_enqueue(&q, 3, false, 0, false);
    event_queue_enqueue(&q, 4, false, 0, false); /* dropped, sets loss flag */

    TEST_ASSERT_TRUE(event_queue_consume_loss_flag(&q));
    TEST_ASSERT_FALSE(event_queue_consume_loss_flag(&q)); /* cleared after first read */
}

/* No delete before ack: ack with the wrong phase must not remove the record. */
static void test_ack_requires_matching_phase(void)
{
    event_queue_enqueue(&q, 1, false /* ACTIVE */, 0, false);
    TEST_ASSERT_FALSE(event_queue_ack(&q, 1, true /* ENDED, wrong phase */));
    TEST_ASSERT_EQUAL(1, event_queue_count(&q));
    TEST_ASSERT_TRUE(event_queue_ack(&q, 1, false));
    TEST_ASSERT_EQUAL(0, event_queue_count(&q));
}

/* FIFO ordering must survive slot reuse after an ack — this is the exact bug
 * class a naive "first free slot = oldest" implementation would hide. */
static void test_peek_oldest_survives_slot_reuse(void)
{
    event_queue_enqueue(&q, 1, false, 0, false); /* fills slot 0 */
    event_queue_enqueue(&q, 2, false, 0, false); /* fills slot 1 */
    event_queue_ack(&q, 1, false);                /* frees slot 0 */
    event_queue_enqueue(&q, 3, false, 0, false); /* reuses slot 0, but is NEWER than event 2 */

    event_record_t out;
    TEST_ASSERT_TRUE(event_queue_peek_oldest(&q, &out));
    TEST_ASSERT_EQUAL(2, out.event_uid); /* not 3, even though 3 landed in the lower-index slot */
}

static void test_was_buffered_is_carried_through_unchanged(void)
{
    event_queue_enqueue(&q, 1, false, 0, true);
    event_record_t out;
    event_queue_peek_oldest(&q, &out);
    TEST_ASSERT_TRUE(out.was_buffered);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_enqueue_and_count);
    RUN_TEST(test_full_queue_drops_new_and_flags_loss_without_evicting);
    RUN_TEST(test_loss_flag_consumed_once);
    RUN_TEST(test_ack_requires_matching_phase);
    RUN_TEST(test_peek_oldest_survives_slot_reuse);
    RUN_TEST(test_was_buffered_is_carried_through_unchanged);
    return UNITY_END();
}
