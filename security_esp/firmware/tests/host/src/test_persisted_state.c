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
    TEST_ASSERT_TRUE(persisted_state_save(&backend, ARM_STATE_ARMED));
    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_OK, persisted_state_load(&backend, &rec));
    TEST_ASSERT_EQUAL(ARM_STATE_ARMED, rec.arm_state);
}

/* T-05 acceptance: corruption must be reported as CORRUPTED, never silently
 * resolved into a guessed ARMED/DISARMED value. */
static void test_corrupted_record_is_reported_not_guessed(void)
{
    TEST_ASSERT_TRUE(persisted_state_save(&backend, ARM_STATE_ARMED));
    TEST_ASSERT_TRUE(fake_storage_corrupt(&fake_ctx, PERSISTED_STATE_KEY));
    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_CORRUPTED, persisted_state_load(&backend, &rec));
}

static void test_save_failure_is_reported(void)
{
    fake_ctx.simulate_save_failure = true;
    TEST_ASSERT_FALSE(persisted_state_save(&backend, ARM_STATE_DISARMED));
}

/* ESP-C-05 gap review item 5: a torn/partial write (storage layer reports
 * success, but only part of the bytes actually landed — e.g. power loss
 * mid-write) must still be caught as CORRUPTED by the checksum, exactly like
 * an explicit corruption. The save() call itself returning true must not be
 * mistaken for the data being trustworthy. */
static void test_torn_write_is_reported_as_corrupted_not_ok(void)
{
    fake_ctx.simulate_torn_write = true;
    TEST_ASSERT_TRUE(persisted_state_save(&backend, ARM_STATE_ARMED)); /* "succeeds" */
    fake_ctx.simulate_torn_write = false;

    persisted_state_record_t rec;
    TEST_ASSERT_EQUAL(PERSISTED_STATE_LOAD_CORRUPTED, persisted_state_load(&backend, &rec));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_load_not_found_on_first_boot);
    RUN_TEST(test_save_then_load_round_trips);
    RUN_TEST(test_corrupted_record_is_reported_not_guessed);
    RUN_TEST(test_save_failure_is_reported);
    RUN_TEST(test_torn_write_is_reported_as_corrupted_not_ok);
    return UNITY_END();
}
