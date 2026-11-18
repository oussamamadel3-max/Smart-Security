#include "unity.h"
#include "persisted_state.h"
#include "fake_storage.h"

static fake_storage_ctx_t fake_ctx;
static storage_backend_t backend;

void setUp(void)
{
    fake_storage_reset(&fake_ctx);
    backend = fake_storage_backend(&fake_ctx);
}
void tearDown(void) {}

static void test_load_not_found_on_first_boot(void)
{
    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_NOT_FOUND, persisted_state_load(&backend, &rec));
}

static void test_save_then_load_round_trips(void)
{
    TEST_ASSERT_TRUE(persisted_state_save(&backend, ARM_STATE_ARMED, 42));
    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_OK, persisted_state_load(&backend, &rec));
    TEST_ASSERT_EQUAL(ARM_STATE_ARMED, rec.arm_state);
    TEST_ASSERT_EQUAL(42, rec.next_event_uid);
}

/* T-05 acceptance: corruption must be reported as CORRUPTED, never silently
 * resolved into a guessed ARMED/DISARMED value. */
static void test_corrupted_record_is_reported_not_guessed(void)
{
    TEST_ASSERT_TRUE(persisted_state_save(&backend, ARM_STATE_ARMED, 1));
    TEST_ASSERT_TRUE(fake_storage_corrupt(&fake_ctx, PERSISTED_STATE_KEY));
    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_CORRUPTED, persisted_state_load(&backend, &rec));
}

static void test_save_failure_is_reported(void)
{
    fake_ctx.simulate_save_failure = true;
    TEST_ASSERT_FALSE(persisted_state_save(&backend, ARM_STATE_DISARMED, 0));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_load_not_found_on_first_boot);
    RUN_TEST(test_save_then_load_round_trips);
    RUN_TEST(test_corrupted_record_is_reported_not_guessed);
    RUN_TEST(test_save_failure_is_reported);
    return UNITY_END();
}
