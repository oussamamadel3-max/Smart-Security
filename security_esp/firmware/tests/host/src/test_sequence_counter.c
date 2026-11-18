#include "unity.h"
#include "sequence_counter.h"
#include "fake_storage.h"

static fake_storage_ctx_t fake_ctx;
static storage_backend_t backend;

void setUp(void)
{
    fake_storage_reset(&fake_ctx);
    backend = fake_storage_backend(&fake_ctx);
}
void tearDown(void) {}

static void test_first_boot_starts_at_one(void)
{
    sequence_counter_t counter;
    TEST_ASSERT_TRUE(sequence_counter_init(&counter, &backend, 10));
    TEST_ASSERT_EQUAL_UINT64(1, sequence_counter_next(&counter));
    TEST_ASSERT_EQUAL_UINT64(2, sequence_counter_next(&counter));
}

static void test_values_strictly_increase_within_a_batch(void)
{
    sequence_counter_t counter;
    sequence_counter_init(&counter, &backend, 10);
    uint64_t prev = sequence_counter_next(&counter);
    for (int i = 0; i < 20; i++) {
        uint64_t next = sequence_counter_next(&counter);
        TEST_ASSERT_TRUE(next > prev);
        prev = next;
    }
}

/* D-05: after a simulated reboot, the sequence must resume strictly after the
 * last persisted ceiling, never reusing a value — even though the previous
 * boot only actually used 2 of its batch of 10. */
static void test_resumes_after_reboot_without_reusing_values(void)
{
    sequence_counter_t boot1;
    sequence_counter_init(&boot1, &backend, 10);
    uint64_t v1 = sequence_counter_next(&boot1); /* 1 */
    uint64_t v2 = sequence_counter_next(&boot1); /* 2 */
    (void)v1;
    (void)v2;
    /* Simulated crash: boot1 is abandoned without using the rest of its
     * reserved batch [1..10]; only the persisted ceiling (10) survives. */

    sequence_counter_t boot2;
    sequence_counter_init(&boot2, &backend, 10);
    uint64_t v3 = sequence_counter_next(&boot2);
    TEST_ASSERT_TRUE(v3 > v2);
    TEST_ASSERT_EQUAL_UINT64(11, v3); /* resumes right after the old ceiling, not after v2 */
}

static void test_batch_exhaustion_persists_a_new_ceiling(void)
{
    sequence_counter_t counter;
    sequence_counter_init(&counter, &backend, 2); /* batch of 2: ceiling becomes 2 */
    sequence_counter_next(&counter); /* 1 */
    sequence_counter_next(&counter); /* 2, exhausts the batch */
    uint64_t v = sequence_counter_next(&counter); /* must trigger a new reservation */
    TEST_ASSERT_EQUAL_UINT64(3, v);

    sequence_counter_record_t rec;
    TEST_ASSERT_TRUE(backend.load(backend.ctx, SEQUENCE_COUNTER_KEY, &rec, sizeof(rec)));
    TEST_ASSERT_TRUE(rec.reserved_ceiling >= 4);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_first_boot_starts_at_one);
    RUN_TEST(test_values_strictly_increase_within_a_batch);
    RUN_TEST(test_resumes_after_reboot_without_reusing_values);
    RUN_TEST(test_batch_exhaustion_persists_a_new_ceiling);
    return UNITY_END();
}
