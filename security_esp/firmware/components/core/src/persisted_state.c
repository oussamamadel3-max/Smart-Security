#include "persisted_state.h"
#include <string.h>

#define PERSISTED_STATE_VERSION 1u

static uint32_t compute_checksum(const persisted_state_record_t *rec)
{
    persisted_state_record_t tmp = *rec;
    tmp.checksum = 0;
    return storage_checksum(&tmp, sizeof(tmp));
}

persisted_state_load_result_t persisted_state_load(const storage_backend_t *backend,
                                                    persisted_state_record_t *out)
{
    persisted_state_record_t rec;
    if (!backend->load(backend->ctx, PERSISTED_STATE_KEY, &rec, sizeof(rec))) {
        return PERSISTED_STATE_LOAD_NOT_FOUND;
    }
    if (rec.version != PERSISTED_STATE_VERSION) {
        return PERSISTED_STATE_LOAD_CORRUPTED;
    }
    if (compute_checksum(&rec) != rec.checksum) {
        return PERSISTED_STATE_LOAD_CORRUPTED;
    }
    *out = rec;
    return PERSISTED_STATE_LOAD_OK;
}

bool persisted_state_save(const storage_backend_t *backend, arm_state_t arm_state, uint32_t next_event_uid)
{
    persisted_state_record_t rec;
    memset(&rec, 0, sizeof(rec));
    rec.version = PERSISTED_STATE_VERSION;
    rec.arm_state = arm_state;
    rec.next_event_uid = next_event_uid;
    rec.checksum = compute_checksum(&rec);
    return backend->save(backend->ctx, PERSISTED_STATE_KEY, &rec, sizeof(rec));
}
