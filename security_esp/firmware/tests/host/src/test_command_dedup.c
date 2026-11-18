#include "unity.h"
#include "command_dedup.h"
#include <stdio.h>

static command_dedup_t dedup;

void setUp(void) { command_dedup_init(&dedup); }
void tearDown(void) {}

static void test_first_seen_is_not_duplicate(void)
{
    TEST_ASSERT_FALSE(command_dedup_check_and_record(&dedup, "aaaa"));
}

static void test_repeat_is_duplicate(void)
{
    command_dedup_check_and_record(&dedup, "aaaa");
    TEST_ASSERT_TRUE(command_dedup_check_and_record(&dedup, "aaaa"));
}

static void test_ring_eventually_forgets_oldest_after_capacity_exceeded(void)
{
    char id[8];
    for (int i = 0; i < COMMAND_DEDUP_CAPACITY; i++) {
        snprintf(id, sizeof(id), "id%d", i);
        TEST_ASSERT_FALSE(command_dedup_check_and_record(&dedup, id));
    }
    /* "id0" was the first recorded; after filling the ring exactly to
     * capacity with new ids, one more new id pushes it out. */
    TEST_ASSERT_FALSE(command_dedup_check_and_record(&dedup, "idNEW"));
    TEST_ASSERT_FALSE(command_dedup_check_and_record(&dedup, "id0"));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_first_seen_is_not_duplicate);
    RUN_TEST(test_repeat_is_duplicate);
    RUN_TEST(test_ring_eventually_forgets_oldest_after_capacity_exceeded);
    return UNITY_END();
}
