#include "nvs_storage_backend.h"
#include "nvs.h"
#include "esp_log.h"

static const char *TAG = "nvs_storage_backend";

static bool nvs_storage_load(void *ctx_v, const char *key, void *out, size_t size)
{
    nvs_storage_ctx_t *ctx = (nvs_storage_ctx_t *)ctx_v;
    nvs_handle_t handle;
    if (nvs_open(ctx->nvs_namespace, NVS_READONLY, &handle) != ESP_OK) {
        return false; /* namespace not created yet = nothing saved yet, not an error */
    }
    size_t required_size = size;
    esp_err_t err = nvs_get_blob(handle, key, out, &required_size);
    nvs_close(handle);
    if (err != ESP_OK) {
        if (err != ESP_ERR_NVS_NOT_FOUND) {
            ESP_LOGW(TAG, "nvs_get_blob(%s) failed: %s", key, esp_err_to_name(err));
        }
        return false;
    }
    /* A size mismatch means the stored blob does not match what the caller
     * expects (e.g. an older schema version) — treat it the same as "not
     * found", never read a partial/misaligned struct into `out`. */
    return required_size == size;
}

static bool nvs_storage_save(void *ctx_v, const char *key, const void *data, size_t size)
{
    nvs_storage_ctx_t *ctx = (nvs_storage_ctx_t *)ctx_v;
    nvs_handle_t handle;
    esp_err_t err = nvs_open(ctx->nvs_namespace, NVS_READWRITE, &handle);
    if (err != ESP_OK) {
        ESP_LOGE(TAG, "nvs_open(%s, RW) failed: %s", ctx->nvs_namespace, esp_err_to_name(err));
        return false;
    }
    err = nvs_set_blob(handle, key, data, size);
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    if (err != ESP_OK) {
        ESP_LOGE(TAG, "nvs_set_blob/commit(%s) failed: %s", key, esp_err_to_name(err));
        return false;
    }
    return true;
}

storage_backend_t nvs_storage_backend_create(nvs_storage_ctx_t *ctx)
{
    storage_backend_t backend;
    backend.load = nvs_storage_load;
    backend.save = nvs_storage_save;
    backend.ctx = ctx;
    return backend;
}
