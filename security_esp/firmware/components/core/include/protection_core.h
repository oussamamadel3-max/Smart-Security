/*
 * T-03/T-04 — local protection state machine, siren priority rules, and the
 * five device commands (ARM, DISARM, MUTE_ALARM, TEST_SIREN, STOP_TEST).
 * Hardware/network independent: no GPIO, no MQTT, no persistence here (T-05
 * owns saving/restoring arm_state and the event-uid counter across reboot).
 *
 * siren_state is a *derived* value (protection_core_siren_state), never stored
 * directly, so it can never drift out of sync with arm_state / current_event /
 * muted / test_active. This is what makes "DISARM -> no siren" (AC-T-02) and
 * "real alarm preempts test" hold by construction instead of by scattered
 * special cases.
 *
 * ASSUMPTION flagged for SRS/integration confirmation: the contract text found
 * in this repo (docs/contracts/state-machines.md, esp-development-tasks.md)
 * does not spell out whether DISARM must clear current_alarm_event_uid. This
 * module keeps the event id alive across DISARM (only sirenState is forced
 * off) because event-lifecycle elsewhere in the contract is sensor-driven
 * ("نهاية الحادثة عند عودة الحساس للوضع الطبيعي"), not command-driven. Treat
 * this as a documented engineering interpretation, not an approved decision.
 */
#ifndef PROTECTION_CORE_H
#define PROTECTION_CORE_H

#include "esp_core_types.h"
#include "alarm_detector.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    PROTECTION_CMD_OK = 0,
    PROTECTION_CMD_REJECTED_TEST_DURING_ALARM,
    PROTECTION_CMD_REJECTED_STOP_TEST_NOT_ACTIVE,
    PROTECTION_CMD_REJECTED_MUTE_EVENT_MISMATCH,
    PROTECTION_CMD_REJECTED_MUTE_NO_ACTIVE_EVENT
} protection_cmd_result_t;

typedef struct {
    arm_state_t arm_state;
    alarm_event_id_t current_event;
    bool muted;
    bool test_active;
    esp_core_time_ms_t test_started_at_ms;
    uint32_t test_duration_ms;   /* D-03 reference value: 30000 ms; caller-configured, not hard-coded here */
    uint32_t next_event_uid;     /* monotonic local counter; T-05 persists/restores this across reboot */
    alarm_detector_state_t detector;
} protection_core_t;

/* arm_state starts DISARMED. test_duration_ms is the siren-test duration in
 * milliseconds (pass the D-03 reference 30000 for the current test-only
 * value). Returns false if detector_config is invalid (see alarm_detector_init). */
bool protection_core_init(protection_core_t *core,
                          const alarm_detector_config_t *detector_config,
                          uint32_t test_duration_ms);

void protection_core_arm(protection_core_t *core);

/* Does not clear current_event or muted — see the ASSUMPTION note above. */
void protection_core_disarm(protection_core_t *core);

/* Ignored entirely while DISARMED (local alarm only fires when ARMED). */
void protection_core_on_sensor_pulse(protection_core_t *core, esp_core_time_ms_t now_ms);

/* Drives cooldown-based event clearing and test-duration expiry. Call at least
 * as often as the smallest of cooldown_ms / test_duration_ms to observe timely
 * transitions. */
void protection_core_tick(protection_core_t *core, esp_core_time_ms_t now_ms);

protection_cmd_result_t protection_core_mute(protection_core_t *core, uint32_t target_event_uid);

protection_cmd_result_t protection_core_start_test(protection_core_t *core, esp_core_time_ms_t now_ms);

protection_cmd_result_t protection_core_stop_test(protection_core_t *core);

/* Derived output — see file header. This is what the siren GPIO driver (T-06
 * port layer) reads to decide whether to energize the siren circuit. */
siren_state_t protection_core_siren_state(const protection_core_t *core);

#ifdef __cplusplus
}
#endif

#endif /* PROTECTION_CORE_H */
