/*
 * Builds STATE JSON per docs/contracts/mqtt/v1/state.schema.json (draft).
 */
#ifndef STATE_JSON_H
#define STATE_JSON_H

#include "esp_core_types.h"
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uint64_t state_sequence;
    uint64_t confirmed_ownership_version;
    arm_state_t arm_state;
    siren_state_t siren_state;
    health_state_t health_state;
    bool has_current_alarm_event_uid;
    uuid128_t current_alarm_event_uid;
    bool clock_trusted;
} state_snapshot_t;

/* Heap-allocated JSON; caller frees with free()/cJSON_free. */
char *state_to_json(const state_snapshot_t *snap,
                    const char *device_id,
                    const char *message_id,
                    const char *boot_id);

#ifdef __cplusplus
}
#endif

#endif /* STATE_JSON_H */
