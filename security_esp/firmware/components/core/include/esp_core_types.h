/*
 * Hardware-independent core types shared across protection/command/storage modules.
 * Enum names and values mirror the approved device-state contract in
 * docs/contracts/state-machines.md (armState / sirenState / healthState) — do not
 * rename these without an integration/G1 review, since the MQTT STATE/COMMAND_ACK
 * payload field values are expected to serialize from these names.
 */
#ifndef ESP_CORE_TYPES_H
#define ESP_CORE_TYPES_H

#include <stdint.h>
#include <stdbool.h>
#include "uuid128.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Monotonic milliseconds since boot. Injected by callers instead of read from a
 * HAL clock directly, so core logic stays a pure function of its inputs and is
 * testable on a host without any ESP-IDF timer. */
typedef uint64_t esp_core_time_ms_t;

typedef enum {
    ARM_STATE_UNKNOWN = 0,
    ARM_STATE_DISARMED,
    ARM_STATE_ARMED
} arm_state_t;

typedef enum {
    SIREN_STATE_OFF = 0,
    SIREN_STATE_ALARM,
    SIREN_STATE_TEST,
    SIREN_STATE_UNKNOWN
} siren_state_t;

typedef enum {
    HEALTH_STATE_UNKNOWN = 0,
    HEALTH_STATE_HEALTHY,
    HEALTH_STATE_FAULT
} health_state_t;

/* Local alarm-event identity. uid is a real UUIDv4 (see uuid128.h),
 * generated fresh when a new event is triggered — only meaningful when
 * is_active is true. Kept stable from first qualifying trigger until the
 * sensor is confirmed back to normal (D-02) — commands (MUTE/DISARM) never
 * clear it on their own.
 *
 * Resolved (was ESP-C-05 gap review item 10 / ESP-REMAINING-WORK item 13):
 * uid used to be a local uint32_t counter with no wire-format
 * compatibility. It is now a real uuid128_t because the backend's
 * authoritative schema already commits to a UUID
 * (security_backEnd/Domain/Entities/Events/AlarmEvent.cs: `public Guid
 * EventUid`) — this was never an open product decision, just an
 * unfinished one. */
typedef struct {
    bool is_active;
    uuid128_t uid;
    esp_core_time_ms_t started_at_ms;
} alarm_event_id_t;

#ifdef __cplusplus
}
#endif

#endif /* ESP_CORE_TYPES_H */
