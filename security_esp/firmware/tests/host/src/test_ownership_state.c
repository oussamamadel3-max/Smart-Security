#include "unity.h"
#include "ownership_state.h"

static ownership_state_t ownership;

void setUp(void) { ownership_state_init(&ownership); }
void tearDown(void) {}

static void test_initial_state_is_unlinked_version_zero(void)
{
    TEST_ASSERT_EQUAL_UINT64(0, ownership.version);
    TEST_ASSERT_FALSE(ownership.is_linked);
}

static void test_apply_sync_updates_version_and_link(void)
{
    ownership_state_apply_sync(&ownership, 3, true);
    TEST_ASSERT_EQUAL_UINT64(3, ownership.version);
    TEST_ASSERT_TRUE(ownership.is_linked);
}

/* "معالجة isLinked=false" must still update the version, not be dropped. */
static void test_unlink_sync_still_updates_version(void)
{
    ownership_state_apply_sync(&ownership, 3, true);
    ownership_state_apply_sync(&ownership, 4, false);
    TEST_ASSERT_EQUAL_UINT64(4, ownership.version);
    TEST_ASSERT_FALSE(ownership.is_linked);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_initial_state_is_unlinked_version_zero);
    RUN_TEST(test_apply_sync_updates_version_and_link);
    RUN_TEST(test_unlink_sync_still_updates_version);
    return UNITY_END();
}
