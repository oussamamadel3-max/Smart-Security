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

/* Behavioural proof for "تحقق من سلوك مزامنة الملكية بعد إعادة التشغيل،
 * وليس من توثيق المتطلب فقط" — not just citing
 * docs/خطة_تطوير_نظام_الأمان_الذكي_v1.0.md §4.4 ("تعاد OWNERSHIP_SYNC عند
 * الاتصال"), but actually exercising the scenario it implies: a device
 * that reboots has NO persisted ownership module to load from (there is no
 * ownership_state_load() anywhere in this codebase, unlike persisted_state
 * / sequence_counter which do have load functions) — a fresh instance
 * created after a simulated reboot starts exactly like first boot, and
 * only a fresh OWNERSHIP_SYNC (as the backend is required to resend)
 * brings it back to the pre-reboot version. */
static void test_reboot_scenario_loses_ownership_until_resynced(void)
{
    ownership_state_t before_reboot;
    ownership_state_init(&before_reboot);
    ownership_state_apply_sync(&before_reboot, 5, true);
    TEST_ASSERT_EQUAL_UINT64(5, before_reboot.version);

    /* Simulated reboot: a brand new instance, nothing loaded from storage
     * (no such load function exists for ownership_state). */
    ownership_state_t after_reboot;
    ownership_state_init(&after_reboot);
    TEST_ASSERT_EQUAL_UINT64(0, after_reboot.version);
    TEST_ASSERT_FALSE(after_reboot.is_linked);

    /* Only after the backend's required resend does it match again. */
    ownership_state_apply_sync(&after_reboot, 5, true);
    TEST_ASSERT_EQUAL_UINT64(5, after_reboot.version);
    TEST_ASSERT_TRUE(after_reboot.is_linked);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_initial_state_is_unlinked_version_zero);
    RUN_TEST(test_apply_sync_updates_version_and_link);
    RUN_TEST(test_unlink_sync_still_updates_version);
    RUN_TEST(test_reboot_scenario_loses_ownership_until_resynced);
    return UNITY_END();
}
