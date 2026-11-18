#include "alarm_detector.h"
#include <string.h>

bool alarm_detector_init(alarm_detector_state_t *state, const alarm_detector_config_t *config)
{
    if (state == NULL || config == NULL) {
        return false;
    }
    if (config->pulses_to_qualify == 0 ||
        config->pulses_to_qualify > ALARM_DETECTOR_MAX_PULSE_HISTORY) {
        return false;
    }
    memset(state, 0, sizeof(*state));
    state->config = *config;
    return true;
}

/* Count pulses in pulse_times_ms that fall within [now_ms - window_ms, now_ms]. */
static uint16_t count_pulses_in_window(const alarm_detector_state_t *state, esp_core_time_ms_t now_ms)
{
    uint16_t count = 0;
    for (uint16_t i = 0; i < state->pulse_count; i++) {
        esp_core_time_ms_t age = now_ms - state->pulse_times_ms[i];
        if (age <= state->config.window_ms) {
            count++;
        }
    }
    return count;
}

alarm_detector_event_t alarm_detector_on_pulse(alarm_detector_state_t *state, esp_core_time_ms_t now_ms)
{
    state->last_pulse_ms = now_ms;
    state->has_last_pulse = true;

    if (state->is_event_active) {
        /* Already alarming: no new eventUid, just keep the trail alive for the
         * cooldown check. We still record the pulse so a sparse but ongoing
         * disturbance doesn't look idle to count_pulses_in_window. */
        if (state->pulse_count < ALARM_DETECTOR_MAX_PULSE_HISTORY) {
            state->pulse_times_ms[state->pulse_count++] = now_ms;
        } else {
            /* Ring-overwrite oldest; history is only used for the qualify
             * check, which is irrelevant once an event is already active. */
            memmove(&state->pulse_times_ms[0], &state->pulse_times_ms[1],
                    (ALARM_DETECTOR_MAX_PULSE_HISTORY - 1) * sizeof(esp_core_time_ms_t));
            state->pulse_times_ms[ALARM_DETECTOR_MAX_PULSE_HISTORY - 1] = now_ms;
        }
        return ALARM_DETECTOR_EVENT_ALREADY_ACTIVE;
    }

    if (state->pulse_count < ALARM_DETECTOR_MAX_PULSE_HISTORY) {
        state->pulse_times_ms[state->pulse_count++] = now_ms;
    } else {
        memmove(&state->pulse_times_ms[0], &state->pulse_times_ms[1],
                (ALARM_DETECTOR_MAX_PULSE_HISTORY - 1) * sizeof(esp_core_time_ms_t));
        state->pulse_times_ms[ALARM_DETECTOR_MAX_PULSE_HISTORY - 1] = now_ms;
    }

    if (count_pulses_in_window(state, now_ms) >= state->config.pulses_to_qualify) {
        state->is_event_active = true;
        return ALARM_DETECTOR_EVENT_TRIGGERED;
    }
    return ALARM_DETECTOR_EVENT_NONE;
}

bool alarm_detector_tick(alarm_detector_state_t *state, esp_core_time_ms_t now_ms)
{
    if (!state->is_event_active || !state->has_last_pulse) {
        return false;
    }
    esp_core_time_ms_t silence = now_ms - state->last_pulse_ms;
    if (silence >= state->config.cooldown_ms) {
        state->is_event_active = false;
        state->pulse_count = 0;
        state->has_last_pulse = false;
        return true;
    }
    return false;
}

bool alarm_detector_is_event_active(const alarm_detector_state_t *state)
{
    return state->is_event_active;
}
