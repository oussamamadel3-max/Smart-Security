/* Host-test fake for storage_backend_t: an in-memory key/blob map. Not used by
 * firmware; test-only (see AGENTS.md: "استخدم Mocks وFakes داخل الاختبارات فقط"). */
#ifndef FAKE_STORAGE_H
#define FAKE_STORAGE_H

#include "storage_backend.h"

#ifdef __cplusplus
extern "C" {
#endif

#define FAKE_STORAGE_MAX_KEYS 8
/* Must fit event_queue_persist blob (EVENT_QUEUE_MAX_CAPACITY records). */
#define FAKE_STORAGE_MAX_BLOB_SIZE 4096

typedef struct {
    char key[32];
    uint8_t data[FAKE_STORAGE_MAX_BLOB_SIZE];
    size_t size;
    bool in_use;
} fake_storage_entry_t;

typedef struct {
    fake_storage_entry_t entries[FAKE_STORAGE_MAX_KEYS];
    /* When true, save() fails without storing anything — simulates a flash
     * write failure / full partition for failure-path tests. */
    bool simulate_save_failure;
    /* When true, save() reports success (returns true, as a caller/power-loss
     * mid-write might still observe) but only the first half of the bytes
     * actually land — simulating a torn/partial flash write. ESP-C-05 gap
     * review item 5 ("ضمان سلامة التخزين عند فشل جزئي في الكتابة"): this
     * exists so persisted_state/sequence_counter tests can prove their
     * checksum catches a torn write even when the storage layer itself does
     * not report an error. */
    bool simulate_torn_write;
} fake_storage_ctx_t;

void fake_storage_reset(fake_storage_ctx_t *ctx);
storage_backend_t fake_storage_backend(fake_storage_ctx_t *ctx);

/* Test helper: corrupt the raw bytes stored under `key` (flips one byte) to
 * exercise checksum-mismatch / corrupted-config paths. Returns false if the
 * key was never saved. */
bool fake_storage_corrupt(fake_storage_ctx_t *ctx, const char *key);

#ifdef __cplusplus
}
#endif

#endif /* FAKE_STORAGE_H */
