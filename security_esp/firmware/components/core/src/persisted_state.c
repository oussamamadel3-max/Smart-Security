#include "persisted_state.h"
#include <string.h>

/* v2 (2026-11-18): dropped the next_event_uid field — event identity moved
 * to a freshly-generated uuid128_t per trigger (see esp_core_types.h /
 * uuid128.h) and is no longer a counter that needs resuming across reboot.
 * Bumped on review ("راجع أثر تغيير بنية persisted_state على إصدار
 * البيانات المخزنة"): record size changed too, so a real NVS backend would
 * likely already reject a v1-sized blob on its own (size mismatch), but the
 * version bump makes the schema change explicit rather than relying on
 * that as an accident of the storage backend's own strictness. No
 * production device has ever stored a v1 record — there is nothing to
 * migrate. */
#define PERSISTED_STATE_VERSION 2u

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

bool persisted_state_save(const storage_backend_t *backend, arm_state_t arm_state)
{
    persisted_state_record_t rec;
    memset(&rec, 0, sizeof(rec));
    rec.version = PERSISTED_STATE_VERSION;
    rec.arm_state = arm_state;
    rec.checksum = compute_checksum(&rec);
    return backend->save(backend->ctx, PERSISTED_STATE_KEY, &rec, sizeof(rec));
}
