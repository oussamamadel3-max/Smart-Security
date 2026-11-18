#include "protection_core.h"
#include <string.h>

bool protection_core_init(protection_core_t *core,
                          const alarm_detector_config_t *detector_config,
                          uint32_t test_duration_ms)
{
    if (core == NULL) {
        return false;
    }
    memset(core, 0, sizeof(*core));
    core->arm_state = ARM_STATE_DISARMED;
    core->test_duration_ms = test_duration_ms;
    return alarm_detector_init(&core->detector, detector_config);
}

void protection_core_arm(protection_core_t *core)
{
    core->arm_state = ARM_STATE_ARMED;
}

void protection_core_disarm(protection_core_t *core)
{
    core->arm_state = ARM_STATE_DISARMED;
}

void protection_core_on_sensor_pulse(protection_core_t *core, esp_core_time_ms_t now_ms)
{
    if (core->arm_state != ARM_STATE_ARMED) {
        return;
    }
    alarm_detector_event_t ev = alarm_detector_on_pulse(&core->detector, now_ms);
    if (ev == ALARM_DETECTOR_EVENT_TRIGGERED) {
        core->current_event.is_active = true;
        core->current_event.uid = ++core->next_event_uid;
        core->current_event.started_at_ms = now_ms;
        core->muted = false;
        core->test_active = false; /* real alarm pre-empts an in-progress test */
    }
}

void protection_core_tick(protection_core_t *core, esp_core_time_ms_t now_ms)
{
    if (alarm_detector_tick(&core->detector, now_ms)) {
        core->current_event.is_active = false;
        core->muted = false;
    }
    if (core->test_active && (now_ms - core->test_started_at_ms) >= core->test_duration_ms) {
        core->test_active = false;
    }
}

protection_cmd_result_t protection_core_mute(protection_core_t *core, uint32_t target_event_uid)
{
    if (!core->current_event.is_active) {
        return PROTECTION_CMD_REJECTED_MUTE_NO_ACTIVE_EVENT;
    }
    if (core->current_event.uid != target_event_uid) {
        return PROTECTION_CMD_REJECTED_MUTE_EVENT_MISMATCH;
    }
    core->muted = true;
    return PROTECTION_CMD_OK;
}

protection_cmd_result_t protection_core_start_test(protection_core_t *core, esp_core_time_ms_t now_ms)
{
    if (core->current_event.is_active) {
        return PROTECTION_CMD_REJECTED_TEST_DURING_ALARM;
    }
    core->test_active = true;
    core->test_started_at_ms = now_ms;
    return PROTECTION_CMD_OK;
}

protection_cmd_result_t protection_core_stop_test(protection_core_t *core)
{
    if (!core->test_active) {
        return PROTECTION_CMD_REJECTED_STOP_TEST_NOT_ACTIVE;
    }
    core->test_active = false;
    return PROTECTION_CMD_OK;
}

siren_state_t protection_core_siren_state(const protection_core_t *core)
{
    if (core->arm_state == ARM_STATE_ARMED && core->current_event.is_active && !core->muted) {
        return SIREN_STATE_ALARM;
    }
    if (core->test_active) {
        return SIREN_STATE_TEST;
    }
    return SIREN_STATE_OFF;
}
