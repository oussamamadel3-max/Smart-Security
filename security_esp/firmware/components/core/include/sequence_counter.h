/*
 * D-05 — stateSequence must keep increasing across reboot and must never be
 * reused, even after an unclean power loss. Writing the counter to flash on
 * every increment would be both slow and bad for flash wear, so this module
 * reserves a batch of future values with a single flash write (the "reserved
 * ceiling"), hands out values from that batch in RAM, and only writes again
 * once the batch is exhausted. On boot, it resumes strictly after the last
 * persisted ceiling and immediately reserves a fresh batch — so even if the
 * previous boot crashed after using value 5 of a [1..100] batch, values
 * 6..100 are simply skipped (never reused), not reassigned.
 *
 * batch_size is a tuning knob (write-wear vs. skipped-values-after-crash) —
 * not yet measured on real flash (D-03/NFR-008 open); do not treat the
 * default here as a production-approved value.
 */
#ifndef SEQUENCE_COUNTER_H
#define SEQUENCE_COUNTER_H

#include "storage_backend.h"

#ifdef __cplusplus
extern "C" {
#endif

#define SEQUENCE_COUNTER_KEY "state_sequence_v1"

typedef struct {
    uint32_t version;
    uint64_t reserved_ceiling; /* highest value ever reserved; never reused */
    uint32_t checksum;
} sequence_counter_record_t;

typedef struct {
    const storage_backend_t *backend;
    uint64_t next_value;
    uint64_t reserved_ceiling;
    uint32_t batch_size;
} sequence_counter_t;

/* Loads the persisted ceiling (treats NOT_FOUND / CORRUPTED the same way: as
 * "no proven ceiling", starting from 0 — a corrupted ceiling must never be
 * interpreted as "ceiling 0 is safe to reuse" if any prior value could have
 * been higher, so a real deployment should fail closed / raise a diagnostic
 * fault here rather than silently resetting; that policy choice is left to
 * the caller, flagged in esp-progress.md as an open item). Immediately
 * reserves and persists a new batch before returning any value. */
bool sequence_counter_init(sequence_counter_t *counter, const storage_backend_t *backend, uint32_t batch_size);

/* Returns the next strictly-increasing value. Transparently persists a new
 * reservation when the current batch is exhausted. */
uint64_t sequence_counter_next(sequence_counter_t *counter);

#ifdef __cplusplus
}
#endif

#endif /* SEQUENCE_COUNTER_H */
