/*
 * Local SoftAP Wi-Fi provisioning (T-08 experimental path).
 *
 * - Opens a temporary AP for SSID/password entry only.
 * - Explicitly rejects any ARM/DISARM/MUTE/TEST control paths.
 * - Closes after success or timeout.
 * - Never logs the Wi-Fi password.
 *
 * This is NOT a production auth product — D-04 procedure details remain open.
 */
#ifndef WIFI_PROVISIONING_AP_H
#define WIFI_PROVISIONING_AP_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*wifi_prov_done_cb_t)(bool saved_ok, void *ctx);

typedef struct {
    const char *ap_ssid;          /* e.g. "SmartSecurity-Setup" — lab label */
    uint32_t timeout_ms;          /* close AP after this even if unused */
    wifi_prov_done_cb_t on_done;
    void *ctx;
} wifi_provisioning_ap_config_t;

bool wifi_provisioning_ap_start(const wifi_provisioning_ap_config_t *cfg);

void wifi_provisioning_ap_stop(void);

bool wifi_provisioning_ap_is_active(void);

#ifdef __cplusplus
}
#endif

#endif /* WIFI_PROVISIONING_AP_H */
