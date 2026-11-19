#include "fake_storage.h"
#include <string.h>

static fake_storage_entry_t *find_entry(fake_storage_ctx_t *ctx, const char *key, bool create_if_missing)
{
    fake_storage_entry_t *free_slot = NULL;
    for (int i = 0; i < FAKE_STORAGE_MAX_KEYS; i++) {
        if (ctx->entries[i].in_use && strcmp(ctx->entries[i].key, key) == 0) {
            return &ctx->entries[i];
        }
        if (!ctx->entries[i].in_use && free_slot == NULL) {
            free_slot = &ctx->entries[i];
        }
    }
    if (create_if_missing && free_slot != NULL) {
        memset(free_slot, 0, sizeof(*free_slot));
        strncpy(free_slot->key, key, sizeof(free_slot->key) - 1);
        free_slot->in_use = true;
        return free_slot;
    }
    return NULL;
}

static bool fake_load(void *ctx_v, const char *key, void *out, size_t size)
{
    fake_storage_ctx_t *ctx = (fake_storage_ctx_t *)ctx_v;
    fake_storage_entry_t *entry = find_entry(ctx, key, false);
    if (entry == NULL || entry->size != size) {
        return false;
    }
    memcpy(out, entry->data, size);
    return true;
}

static bool fake_save(void *ctx_v, const char *key, const void *data, size_t size)
{
    fake_storage_ctx_t *ctx = (fake_storage_ctx_t *)ctx_v;
    if (ctx->simulate_save_failure) {
        return false;
    }
    if (size > FAKE_STORAGE_MAX_BLOB_SIZE) {
        return false;
    }
    fake_storage_entry_t *entry = find_entry(ctx, key, true);
    if (entry == NULL) {
        return false;
    }
    if (ctx->simulate_torn_write) {
        size_t torn_size = size / 2;
        memset(entry->data, 0, size);
        memcpy(entry->data, data, torn_size);
        entry->size = size; /* caller still expects to read back `size` bytes */
        return true;        /* torn writes still "succeed" from the caller's point of view */
    }
    memcpy(entry->data, data, size);
    entry->size = size;
    return true;
}

void fake_storage_reset(fake_storage_ctx_t *ctx)
{
    memset(ctx, 0, sizeof(*ctx));
}

storage_backend_t fake_storage_backend(fake_storage_ctx_t *ctx)
{
    storage_backend_t backend;
    backend.load = fake_load;
    backend.save = fake_save;
    backend.ctx = ctx;
    return backend;
}

bool fake_storage_corrupt(fake_storage_ctx_t *ctx, const char *key)
{
    fake_storage_entry_t *entry = find_entry(ctx, key, false);
    if (entry == NULL || entry->size == 0) {
        return false;
    }
    entry->data[0] ^= 0xFF;
    return true;
}
