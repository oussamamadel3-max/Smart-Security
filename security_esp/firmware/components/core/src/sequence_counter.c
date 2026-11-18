#include "sequence_counter.h"
#include <string.h>

#define SEQUENCE_COUNTER_VERSION 1u

static uint32_t compute_checksum(const sequence_counter_record_t *rec)
{
    sequence_counter_record_t tmp = *rec;
    tmp.checksum = 0;
    return storage_checksum(&tmp, sizeof(tmp));
}

static bool persist_ceiling(const storage_backend_t *backend, uint64_t ceiling)
{
    sequence_counter_record_t rec;
    memset(&rec, 0, sizeof(rec));
    rec.version = SEQUENCE_COUNTER_VERSION;
    rec.reserved_ceiling = ceiling;
    rec.checksum = compute_checksum(&rec);
    return backend->save(backend->ctx, SEQUENCE_COUNTER_KEY, &rec, sizeof(rec));
}

bool sequence_counter_init(sequence_counter_t *counter, const storage_backend_t *backend, uint32_t batch_size)
{
    if (counter == NULL || backend == NULL || batch_size == 0) {
        return false;
    }
    memset(counter, 0, sizeof(*counter));
    counter->backend = backend;
    counter->batch_size = batch_size;

    sequence_counter_record_t rec;
    uint64_t last_proven_ceiling = 0;
    if (backend->load(backend->ctx, SEQUENCE_COUNTER_KEY, &rec, sizeof(rec)) &&
        rec.version == SEQUENCE_COUNTER_VERSION &&
        compute_checksum(&rec) == rec.checksum) {
        last_proven_ceiling = rec.reserved_ceiling;
    }
    /* NOT_FOUND or CORRUPTED both fall back to last_proven_ceiling == 0 here;
     * see the header comment on why that is a flagged open policy choice,
     * not a silently "safe" default for a production deployment. */

    counter->next_value = last_proven_ceiling + 1;
    uint64_t new_ceiling = last_proven_ceiling + counter->batch_size;
    if (!persist_ceiling(backend, new_ceiling)) {
        return false;
    }
    counter->reserved_ceiling = new_ceiling;
    return true;
}

uint64_t sequence_counter_next(sequence_counter_t *counter)
{
    if (counter->next_value > counter->reserved_ceiling) {
        uint64_t new_ceiling = counter->reserved_ceiling + counter->batch_size;
        /* Best-effort: if the flash write fails, still hand out the value
         * in RAM rather than deadlocking the caller. A write failure here
         * is a diagnostics/fault condition for the device, not something
         * this pure module escalates on its own. */
        if (persist_ceiling(counter->backend, new_ceiling)) {
            counter->reserved_ceiling = new_ceiling;
        }
    }
    return counter->next_value++;
}
