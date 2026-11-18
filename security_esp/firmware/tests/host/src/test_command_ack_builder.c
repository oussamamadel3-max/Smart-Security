#include "unity.h"
#include "command_ack_builder.h"

static protection_core_t core;
static ownership_state_t ownership;

void setUp(void)
{
    alarm_detector_config_t cfg = { .pulses_to_qualify = 2, .window_ms = 1000, .cooldown_ms = 3000 };
    protection_core_init(&core, &cfg, 30000);
    ownership_state_init(&ownership);
    ownership_state_apply_sync(&ownership, 7, true);
}
void tearDown(void) {}

/* D-09: ACK must carry the FULL actual state, not just a "done" flag. */
static void test_ack_carries_full_actual_state_after_arm(void)
{
    protection_core_arm(&core);
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_HEALTHY, &ownership, true, 42, true, NULL);

    TEST_ASSERT_EQUAL(ARM_STATE_ARMED, ack.arm_state);
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, ack.siren_state);
    TEST_ASSERT_EQUAL(HEALTH_STATE_HEALTHY, ack.health_state);
    TEST_ASSERT_FALSE(ack.has_current_alarm_event_uid);
    TEST_ASSERT_EQUAL_UINT64(7, ack.ownership_version);
    TEST_ASSERT_EQUAL_UINT64(7, ack.confirmed_ownership_version);
    TEST_ASSERT_TRUE(ack.clock_trusted);
    TEST_ASSERT_EQUAL_UINT64(42, ack.result_state_sequence);
    TEST_ASSERT_TRUE(ack.executed);
    TEST_ASSERT_NULL(ack.reject_code);
}

static void test_ack_reflects_active_alarm_event_uid(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_UNKNOWN, &ownership, true, 1, true, NULL);

    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, ack.siren_state);
    TEST_ASSERT_TRUE(ack.has_current_alarm_event_uid);
    TEST_ASSERT_EQUAL(core.current_event.uid, ack.current_alarm_event_uid);
}

static void test_rejected_ack_carries_reject_code(void)
{
    command_ack_t ack = command_ack_build(&core, HEALTH_STATE_UNKNOWN, &ownership, true, 1, false, "OWNERSHIP_MISMATCH");
    TEST_ASSERT_FALSE(ack.executed);
    TEST_ASSERT_EQUAL_STRING("OWNERSHIP_MISMATCH", ack.reject_code);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_ack_carries_full_actual_state_after_arm);
    RUN_TEST(test_ack_reflects_active_alarm_event_uid);
    RUN_TEST(test_rejected_ack_carries_reject_code);
    return UNITY_END();
}
