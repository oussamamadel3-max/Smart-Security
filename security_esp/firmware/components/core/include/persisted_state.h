/*
 * T-05 — last confirmed protection state across reboot.
 *
 * On corruption (checksum mismatch) the caller MUST treat the restored
 * arm_state as ARM_STATE_UNKNOWN, never guess ARMED or DISARMED (T-05
 * acceptance: "لا يُستنتج من UNKNOWN وحده أنه ARM أو DISARM"). What the device
 * should then do locally (keep siren silent? keep protection engaged?) is an
 * explicit open G0 safety-policy decision — this module only reports the
 * corruption; it does not choose a fallback behaviour.
 */
#ifndef PERSISTED_STATE_H
#define PERSISTED_STATE_H

#include "storage_backend.h"
#include "esp_core_types.h"

#ifdef __cplusplus
extern "C" {
#endif

#define PERSISTED_STATE_KEY "protection_state_v1"

typedef struct {
    uint32_t version;
    arm_state_t arm_state;      /* last CONFIRMED value only; never store UNKNOWN as if it were confirmed */
    uint32_t next_event_uid;
    uint32_t checksum;          /* storage_checksum() over the fields above, checksum field excluded */
} persisted_state_record_t;

typedef enum {
    PERSISTED_STATE_LOAD_OK = 0,
    PERSISTED_STATE_LOAD_NOT_FOUND,   /* first boot / never saved: not a corruption */
    PERSISTED_STATE_LOAD_CORRUPTED    /* checksum mismatch: treat arm_state as UNKNOWN */
} persisted_state_load_result_t;

persisted_state_load_result_t persisted_state_load(const storage_backend_t *backend,
                                                    persisted_state_record_t *out);

/* Saves arm_state as CONFIRMED. Never call this with arm_state == ARM_STATE_UNKNOWN. */
bool persisted_state_save(const storage_backend_t *backend, arm_state_t arm_state, uint32_t next_event_uid);

#ifdef __cplusplus
}
#endif

#endif /* PERSISTED_STATE_H */
