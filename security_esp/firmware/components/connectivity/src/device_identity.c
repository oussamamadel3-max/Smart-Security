#include "device_identity.h"
#include "nvs.h"
#include "esp_log.h"
#include <stdio.h>
#include <string.h>

static const char *TAG = "device_identity";
static const char *NVS_NS = "dev_id";
static const char *NVS_KEY = "device_uuid";

bool device_identity_init(device_identity_t *out,
                          void (*rng_fill)(void *ctx, uint8_t *buf, size_t len),
                          void *rng_ctx)
{
    if (out == NULL || rng_fill == NULL) {
        return false;
    }
    memset(out, 0, sizeof(*out));

    nvs_handle_t handle;
    esp_err_t err = nvs_open(NVS_NS, NVS_READWRITE, &handle);
    if (err != ESP_OK) {
        ESP_LOGE(TAG, "nvs_open failed: %s", esp_err_to_name(err));
        return false;
    }

    size_t len = DEVICE_ID_STR_LEN + 1;
    err = nvs_get_str(handle, NVS_KEY, out->device_id, &len);
    if (err == ESP_OK) {
        uuid128_t parsed;
        if (!uuid128_from_string(out->device_id, &parsed)) {
            ESP_LOGW(TAG, "stored deviceId malformed — regenerating");
            err = ESP_ERR_NVS_NOT_FOUND;
        }
    }

    if (err == ESP_ERR_NVS_NOT_FOUND) {
        uuid128_t id = uuid128_generate_v4(rng_fill, rng_ctx);
        uuid128_to_string(&id, out->device_id);
        err = nvs_set_str(handle, NVS_KEY, out->device_id);
        if (err == ESP_OK) {
            err = nvs_commit(handle);
        }
        if (err != ESP_OK) {
            ESP_LOGE(TAG, "persist deviceId failed: %s", esp_err_to_name(err));
            nvs_close(handle);
            return false;
        }
        ESP_LOGI(TAG, "generated and stored new deviceId");
    } else if (err != ESP_OK) {
        ESP_LOGE(TAG, "nvs_get_str failed: %s", esp_err_to_name(err));
        nvs_close(handle);
        return false;
    }
    nvs_close(handle);

    uuid128_t boot = uuid128_generate_v4(rng_fill, rng_ctx);
    char boot_uuid[37];
    uuid128_to_string(&boot, boot_uuid);
    snprintf(out->boot_id, sizeof(out->boot_id), "boot-%s", boot_uuid);
    return true;
}
