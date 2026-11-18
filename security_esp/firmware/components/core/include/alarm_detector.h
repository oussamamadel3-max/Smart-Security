/*
 * D-02 — alarm qualification rule for the SW-420 vibration sensor.
 *
 * Pure, hardware-independent: the caller feeds in debounced digital-edge pulses
 * with a timestamp; this module decides whether that sequence of pulses
 * qualifies as one tamper/alarm event, and when the sensor is considered back to
 * normal. It does not read GPIO, does not know about Wi-Fi/MQTT, and does not
 * decide whether the siren is allowed to sound (that is protection_core's job,
 * which also accounts for arm_state and MUTE).
 *
 * Thresholds (pulses_to_qualify / window_ms / cooldown_ms) are TEST-ONLY
 * reference values until D-02/D-03 are calibrated on real hardware (see
 * security_esp/docs/HARDWARE-DECISIONS-D01-D07.md). Never hard-code a guessed
 * production threshold here without updating that document first.
 */
#ifndef ALARM_DETECTOR_H
#define ALARM_DETECTOR_H

#include "esp_core_types.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uint16_t pulses_to_qualify;   /* N: minimum qualifying pulses within window_ms */
    uint32_t window_ms;           /* T: sliding window the N pulses must fall within */
    uint32_t cooldown_ms;         /* silence duration required before "back to normal" */
    uint16_t pulse_history_cap;   /* capacity of the internal pulse timestamp ring; must be >= pulses_to_qualify */
} alarm_detector_config_t;

typedef enum {
    ALARM_DETECTOR_EVENT_NONE = 0,     /* pulse recorded, not yet qualifying */
    ALARM_DETECTOR_EVENT_TRIGGERED,    /* this pulse made the sequence qualify (alarm should start/continue) */
    ALARM_DETECTOR_EVENT_ALREADY_ACTIVE /* an event is already active; pulse extends it, no new trigger */
} alarm_detector_event_t;

/* Fixed-capacity: capacity is a compile-time bound so no dynamic allocation is
 * needed on the device. */
#define ALARM_DETECTOR_MAX_PULSE_HISTORY 16

typedef struct {
    alarm_detector_config_t config;
    esp_core_time_ms_t pulse_times_ms[ALARM_DETECTOR_MAX_PULSE_HISTORY];
    uint16_t pulse_count;          /* number of valid entries in pulse_times_ms */
    bool is_event_active;
    esp_core_time_ms_t last_pulse_ms;
    bool has_last_pulse;
} alarm_detector_state_t;

/* Returns false if config.pulses_to_qualify > ALARM_DETECTOR_MAX_PULSE_HISTORY
 * (configuration error — caller must not proceed with an invalid detector). */
bool alarm_detector_init(alarm_detector_state_t *state, const alarm_detector_config_t *config);

/* Feed one debounced digital pulse. Returns the qualification outcome for this
 * pulse. Pulses while an event is already active are ignored for re-triggering
 * logic (no second eventUid) but still refresh last_pulse_ms, which is what the
 * cooldown check in alarm_detector_tick needs to detect "back to normal". */
alarm_detector_event_t alarm_detector_on_pulse(alarm_detector_state_t *state, esp_core_time_ms_t now_ms);

/* Call periodically (or at least once after the expected cooldown_ms has
 * elapsed since the last pulse) to detect that the sensor returned to normal.
 * Returns true exactly once when an active event transitions to inactive. */
bool alarm_detector_tick(alarm_detector_state_t *state, esp_core_time_ms_t now_ms);

bool alarm_detector_is_event_active(const alarm_detector_state_t *state);

#ifdef __cplusplus
}
#endif

#endif /* ALARM_DETECTOR_H */
