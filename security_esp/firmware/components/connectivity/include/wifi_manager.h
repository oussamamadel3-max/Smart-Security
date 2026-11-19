/*
 * Experimental STA Wi-Fi manager (ESP-IDF esp_wifi). Credentials come from
 * NVS (written by local SoftAP provisioning) — never from source code.
 * Automatic reconnect is handled by ESP-IDF WIFI_EVENT_STA_DISCONNECTED.
 */
#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*wifi_manager_connected_cb_t)(void *ctx);
typedef void (*wifi_manager_disconnected_cb_t)(void *ctx);

typedef struct {
    wifi_manager_connected_cb_t on_connected;
    wifi_manager_disconnected_cb_t on_disconnected;
    void *ctx;
} wifi_manager_callbacks_t;

bool wifi_manager_init(const wifi_manager_callbacks_t *cbs);

/* Returns false if SSID not provisioned yet (caller should start SoftAP). */
bool wifi_manager_has_credentials(void);

bool wifi_manager_start_sta(void);

bool wifi_manager_save_credentials(const char *ssid, const char *password);

void wifi_manager_clear_credentials(void);

bool wifi_manager_is_connected(void);

#ifdef __cplusplus
}
#endif

#endif /* WIFI_MANAGER_H */
