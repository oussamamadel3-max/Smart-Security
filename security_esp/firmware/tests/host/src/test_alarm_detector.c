#include "unity.h"
#include "alarm_detector.h"

static alarm_detector_state_t det;

static alarm_detector_config_t test_config(void)
{
    alarm_detector_config_t cfg = {
        .pulses_to_qualify = 3,
        .window_ms = 2000,
        .cooldown_ms = 5000,
        .pulse_history_cap = ALARM_DETECTOR_MAX_PULSE_HISTORY
    };
    return cfg;
}

void setUp(void) {}
void tearDown(void) {}

static void test_init_rejects_zero_threshold(void)
{
    alarm_detector_config_t cfg = test_config();
    cfg.pulses_to_qualify = 0;
    TEST_ASSERT_FALSE(alarm_detector_init(&det, &cfg));
}

static void test_init_rejects_threshold_above_capacity(void)
{
    alarm_detector_config_t cfg = test_config();
    cfg.pulses_to_qualify = ALARM_DETECTOR_MAX_PULSE_HISTORY + 1;
    TEST_ASSERT_FALSE(alarm_detector_init(&det, &cfg));
}

static void test_single_pulse_does_not_qualify(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, 1000));
    TEST_ASSERT_FALSE(alarm_detector_is_event_active(&det));
}

static void test_three_pulses_within_window_triggers(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, 1000));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, 1500));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_TRIGGERED, alarm_detector_on_pulse(&det, 2000));
    TEST_ASSERT_TRUE(alarm_detector_is_event_active(&det));
}

static void test_pulses_outside_window_do_not_accumulate(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, 0));
    /* Second pulse 3s later: first pulse (age 3000) is outside window_ms=2000. */
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, 3000));
    TEST_ASSERT_FALSE(alarm_detector_is_event_active(&det));
}

static void test_already_active_does_not_retrigger(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    alarm_detector_on_pulse(&det, 0);
    alarm_detector_on_pulse(&det, 500);
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_TRIGGERED, alarm_detector_on_pulse(&det, 1000));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_ALREADY_ACTIVE, alarm_detector_on_pulse(&det, 1500));
}

static void test_tick_clears_event_after_cooldown(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    alarm_detector_on_pulse(&det, 0);
    alarm_detector_on_pulse(&det, 500);
    alarm_detector_on_pulse(&det, 1000);
    TEST_ASSERT_TRUE(alarm_detector_is_event_active(&det));

    TEST_ASSERT_FALSE(alarm_detector_tick(&det, 1000 + 4000)); /* before cooldown_ms=5000 */
    TEST_ASSERT_TRUE(alarm_detector_is_event_active(&det));

    TEST_ASSERT_TRUE(alarm_detector_tick(&det, 1000 + 5000)); /* cooldown elapsed */
    TEST_ASSERT_FALSE(alarm_detector_is_event_active(&det));
}

static void test_new_event_possible_after_cooldown(void)
{
    alarm_detector_config_t cfg = test_config();
    TEST_ASSERT_TRUE(alarm_detector_init(&det, &cfg));
    alarm_detector_on_pulse(&det, 0);
    alarm_detector_on_pulse(&det, 500);
    alarm_detector_on_pulse(&det, 1000);
    alarm_detector_tick(&det, 1000 + cfg.cooldown_ms);
    TEST_ASSERT_FALSE(alarm_detector_is_event_active(&det));

    esp_core_time_ms_t base = 1000 + cfg.cooldown_ms + 1000;
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, base));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_NONE, alarm_detector_on_pulse(&det, base + 200));
    TEST_ASSERT_EQUAL(ALARM_DETECTOR_EVENT_TRIGGERED, alarm_detector_on_pulse(&det, base + 400));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_init_rejects_zero_threshold);
    RUN_TEST(test_init_rejects_threshold_above_capacity);
    RUN_TEST(test_single_pulse_does_not_qualify);
    RUN_TEST(test_three_pulses_within_window_triggers);
    RUN_TEST(test_pulses_outside_window_do_not_accumulate);
    RUN_TEST(test_already_active_does_not_retrigger);
    RUN_TEST(test_tick_clears_event_after_cooldown);
    RUN_TEST(test_new_event_possible_after_cooldown);
    return UNITY_END();
}
