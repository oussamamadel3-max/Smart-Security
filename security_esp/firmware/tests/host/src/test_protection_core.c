#include "unity.h"
#include "protection_core.h"

static protection_core_t core;

static alarm_detector_config_t test_detector_config(void)
{
    alarm_detector_config_t cfg = { .pulses_to_qualify = 2, .window_ms = 1000, .cooldown_ms = 3000 };
    return cfg;
}

void setUp(void)
{
    alarm_detector_config_t cfg = test_detector_config();
    TEST_ASSERT_TRUE(protection_core_init(&core, &cfg, 30000 /* D-03 reference siren-test duration */));
}
void tearDown(void) {}

/* AC-T-01 equivalent at pure-logic level: ARMED + qualifying pulses -> siren
 * ALARM, entirely without any notion of network/MQTT (this module has none).
 * Real AC-T-01 acceptance still requires real hardware + actual network
 * isolation, which this test does not claim to provide. */
static void test_armed_alarm_triggers_siren(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, protection_core_siren_state(&core));
    TEST_ASSERT_TRUE(core.current_event.is_active);
}

/* AC-T-02 equivalent: pulses while DISARMED never reach the detector, and an
 * already-sounding alarm goes silent the instant DISARM is applied. */
static void test_disarmed_never_sounds(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, protection_core_siren_state(&core));

    protection_core_disarm(&core);
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));

    /* Further pulses while disarmed must not bring the siren back. */
    protection_core_on_sensor_pulse(&core, 1000);
    protection_core_on_sensor_pulse(&core, 1500);
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));
}

static void test_pulses_ignored_while_disarmed(void)
{
    /* Never armed at all. */
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    TEST_ASSERT_FALSE(core.current_event.is_active);
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));
}

/* MUTE_ALARM must stop the sound, must NOT disarm, and must NOT clear the
 * event id (sirenState=OFF does not mean the incident ended). */
static void test_mute_silences_without_disarming_or_clearing_event(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    uint32_t uid = core.current_event.uid;

    TEST_ASSERT_EQUAL(PROTECTION_CMD_OK, protection_core_mute(&core, uid));
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));
    TEST_ASSERT_EQUAL(ARM_STATE_ARMED, core.arm_state);
    TEST_ASSERT_TRUE(core.current_event.is_active);
    TEST_ASSERT_EQUAL(uid, core.current_event.uid);
}

/* AC-T-18 equivalent: MUTE with a mismatched/stale targetAlarmEventUid is
 * rejected, not silently applied to whatever event happens to be active. */
static void test_mute_rejects_event_uid_mismatch(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    uint32_t real_uid = core.current_event.uid;

    TEST_ASSERT_EQUAL(PROTECTION_CMD_REJECTED_MUTE_EVENT_MISMATCH,
                      protection_core_mute(&core, real_uid + 999));
    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, protection_core_siren_state(&core)); /* unaffected by rejected mute */
}

static void test_mute_rejects_when_no_active_event(void)
{
    TEST_ASSERT_EQUAL(PROTECTION_CMD_REJECTED_MUTE_NO_ACTIVE_EVENT, protection_core_mute(&core, 1));
}

/* AC-T-22 equivalent: event id survives mute and keeps being "the same
 * incident" while the signal persists, until the sensor actually goes quiet
 * for the cooldown duration. */
static void test_event_uid_persists_through_mute_until_sensor_normal(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    uint32_t uid = core.current_event.uid;
    protection_core_mute(&core, uid);

    protection_core_on_sensor_pulse(&core, 1000); /* signal continues while muted */
    TEST_ASSERT_EQUAL(uid, core.current_event.uid);
    TEST_ASSERT_TRUE(core.current_event.is_active);

    protection_core_tick(&core, 1000 + 3000); /* cooldown elapses -> sensor "back to normal" */
    TEST_ASSERT_FALSE(core.current_event.is_active);
}

/* "رفض اختبار الصفارة أثناء الإنذار الحقيقي" */
static void test_test_siren_rejected_during_real_alarm(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    TEST_ASSERT_EQUAL(PROTECTION_CMD_REJECTED_TEST_DURING_ALARM, protection_core_start_test(&core, 600));
}

/* "أولوية الإنذار الحقيقي على الاختبار": a real alarm pre-empts an
 * in-progress test instead of being blocked by it. */
static void test_real_alarm_preempts_in_progress_test(void)
{
    TEST_ASSERT_EQUAL(PROTECTION_CMD_OK, protection_core_start_test(&core, 0));
    TEST_ASSERT_EQUAL(SIREN_STATE_TEST, protection_core_siren_state(&core));

    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 100);
    protection_core_on_sensor_pulse(&core, 200);
    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, protection_core_siren_state(&core));
    TEST_ASSERT_FALSE(core.test_active);
}

/* "STOP_TEST لا يوقف الإنذار الحقيقي": while a real alarm is sounding,
 * STOP_TEST (no test is active) is rejected and the alarm is untouched. */
static void test_stop_test_does_not_affect_real_alarm(void)
{
    protection_core_arm(&core);
    protection_core_on_sensor_pulse(&core, 0);
    protection_core_on_sensor_pulse(&core, 500);
    TEST_ASSERT_EQUAL(PROTECTION_CMD_REJECTED_STOP_TEST_NOT_ACTIVE, protection_core_stop_test(&core));
    TEST_ASSERT_EQUAL(SIREN_STATE_ALARM, protection_core_siren_state(&core));
}

static void test_stop_test_rejected_when_not_active(void)
{
    TEST_ASSERT_EQUAL(PROTECTION_CMD_REJECTED_STOP_TEST_NOT_ACTIVE, protection_core_stop_test(&core));
}

static void test_stop_test_ends_test_early(void)
{
    protection_core_start_test(&core, 0);
    TEST_ASSERT_EQUAL(SIREN_STATE_TEST, protection_core_siren_state(&core));
    TEST_ASSERT_EQUAL(PROTECTION_CMD_OK, protection_core_stop_test(&core));
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));
}

static void test_test_duration_expires_on_its_own(void)
{
    protection_core_start_test(&core, 0);
    protection_core_tick(&core, 29999);
    TEST_ASSERT_EQUAL(SIREN_STATE_TEST, protection_core_siren_state(&core));
    protection_core_tick(&core, 30000);
    TEST_ASSERT_EQUAL(SIREN_STATE_OFF, protection_core_siren_state(&core));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_armed_alarm_triggers_siren);
    RUN_TEST(test_disarmed_never_sounds);
    RUN_TEST(test_pulses_ignored_while_disarmed);
    RUN_TEST(test_mute_silences_without_disarming_or_clearing_event);
    RUN_TEST(test_mute_rejects_event_uid_mismatch);
    RUN_TEST(test_mute_rejects_when_no_active_event);
    RUN_TEST(test_event_uid_persists_through_mute_until_sensor_normal);
    RUN_TEST(test_test_siren_rejected_during_real_alarm);
    RUN_TEST(test_real_alarm_preempts_in_progress_test);
    RUN_TEST(test_stop_test_does_not_affect_real_alarm);
    RUN_TEST(test_stop_test_rejected_when_not_active);
    RUN_TEST(test_stop_test_ends_test_early);
    RUN_TEST(test_test_duration_expires_on_its_own);
    return UNITY_END();
}
