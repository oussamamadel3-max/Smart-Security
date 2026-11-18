/*
 * T-05 — minimal key/blob persistence interface. Mirrors NVS get/set-blob
 * semantics closely enough that a real ESP-IDF NVS implementation is a thin
 * adapter (T-06 port layer, not written here — no ESP-IDF available in this
 * environment). Host tests use fakes/fake_storage.c, an in-memory map.
 *
 * `load` returning false means "key not found or read error" — callers must
 * not treat that as "confirmed empty/zero", only as "no value available".
 */
#ifndef STORAGE_BACKEND_H
#define STORAGE_BACKEND_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef bool (*storage_load_fn_t)(void *ctx, const char *key, void *out, size_t size);
typedef bool (*storage_save_fn_t)(void *ctx, const char *key, const void *data, size_t size);

typedef struct {
    storage_load_fn_t load;
    storage_save_fn_t save;
    void *ctx;
} storage_backend_t;

/* Simple additive/rotate checksum — good enough to detect flash corruption or
 * a torn write, not a cryptographic integrity guarantee. */
uint32_t storage_checksum(const void *data, size_t size);

#ifdef __cplusplus
}
#endif

#endif /* STORAGE_BACKEND_H */
