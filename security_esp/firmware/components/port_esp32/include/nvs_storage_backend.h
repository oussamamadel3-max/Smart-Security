/*
 * ESP-C-03 — real NVS implementation of core/storage_backend.h, so
 * persisted_state.h and sequence_counter.h (both already unit-tested
 * against a host fake, see tests/host/fakes/fake_storage.c) can run on
 * real hardware without any change to their own code.
 *
 * Caller must call nvs_flash_init() once (main/app_main.c does this)
 * before using a backend created here.
 */
#ifndef NVS_STORAGE_BACKEND_H
#define NVS_STORAGE_BACKEND_H

#include "storage_backend.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    const char *nvs_namespace; /* NVS namespace name, max 15 chars (NVS limit) */
} nvs_storage_ctx_t;

/* ctx must outlive the returned storage_backend_t (it is stored by pointer,
 * not copied — matches storage_backend_t's own ctx convention). */
storage_backend_t nvs_storage_backend_create(nvs_storage_ctx_t *ctx);

#ifdef __cplusplus
}
#endif

#endif /* NVS_STORAGE_BACKEND_H */
