#include "wifi_manager.h"
#include "esp_wifi.h"
#include "esp_event.h"
#include "esp_log.h"
#include "esp_netif.h"
#include "nvs.h"
#include <string.h>

static const char *TAG = "wifi_manager";
static const char *NVS_NS = "wifi_cfg";
static const char *NVS_SSID = "ssid";
static const char *NVS_PASS = "pass";

static wifi_manager_callbacks_t s_cbs;
static bool s_connected;
static bool s_inited;

static void on_wifi_event(void *arg, esp_event_base_t base, int32_t id, void *data)
{
    (void)arg;
    (void)base;
    (void)data;
    if (id == WIFI_EVENT_STA_START) {
        esp_wifi_connect();
    } else if (id == WIFI_EVENT_STA_DISCONNECTED) {
        s_connected = false;
        ESP_LOGW(TAG, "STA disconnected — scheduling reconnect");
        if (s_cbs.on_disconnected) {
            s_cbs.on_disconnected(s_cbs.ctx);
        }
        esp_wifi_connect(); /* ESP-IDF automatic reconnection path */
    }
}

static void on_ip_event(void *arg, esp_event_base_t base, int32_t id, void *data)
{
    (void)arg;
    (void)base;
    (void)data;
    if (id == IP_EVENT_STA_GOT_IP) {
        s_connected = true;
        ESP_LOGI(TAG, "STA got IP");
        if (s_cbs.on_connected) {
            s_cbs.on_connected(s_cbs.ctx);
        }
    }
}

bool wifi_manager_init(const wifi_manager_callbacks_t *cbs)
{
    if (s_inited) {
        return true;
    }
    if (cbs != NULL) {
        s_cbs = *cbs;
    } else {
        memset(&s_cbs, 0, sizeof(s_cbs));
    }

    ESP_ERROR_CHECK(esp_netif_init());
    ESP_ERROR_CHECK(esp_event_loop_create_default());
    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    ESP_ERROR_CHECK(esp_event_handler_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &on_wifi_event, NULL));
    ESP_ERROR_CHECK(esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &on_ip_event, NULL));
    ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    s_inited = true;
    return true;
}

bool wifi_manager_has_credentials(void)
{
    nvs_handle_t h;
    if (nvs_open(NVS_NS, NVS_READONLY, &h) != ESP_OK) {
        return false;
    }
    char ssid[33];
    size_t len = sizeof(ssid);
    esp_err_t err = nvs_get_str(h, NVS_SSID, ssid, &len);
    nvs_close(h);
    return err == ESP_OK && ssid[0] != '\0';
}

bool wifi_manager_save_credentials(const char *ssid, const char *password)
{
    if (ssid == NULL || ssid[0] == '\0' || strlen(ssid) > 32) {
        return false;
    }
    if (password == NULL) {
        password = "";
    }
    if (strlen(password) > 63) {
        return false;
    }
    nvs_handle_t h;
    if (nvs_open(NVS_NS, NVS_READWRITE, &h) != ESP_OK) {
        return false;
    }
    esp_err_t err = nvs_set_str(h, NVS_SSID, ssid);
    if (err == ESP_OK) {
        err = nvs_set_str(h, NVS_PASS, password);
    }
    if (err == ESP_OK) {
        err = nvs_commit(h);
    }
    nvs_close(h);
    return err == ESP_OK;
}

void wifi_manager_clear_credentials(void)
{
    nvs_handle_t h;
    if (nvs_open(NVS_NS, NVS_READWRITE, &h) != ESP_OK) {
        return;
    }
    nvs_erase_key(h, NVS_SSID);
    nvs_erase_key(h, NVS_PASS);
    nvs_commit(h);
    nvs_close(h);
}

bool wifi_manager_start_sta(void)
{
    if (!s_inited || !wifi_manager_has_credentials()) {
        return false;
    }
    nvs_handle_t h;
    if (nvs_open(NVS_NS, NVS_READONLY, &h) != ESP_OK) {
        return false;
    }
    wifi_config_t conf;
    memset(&conf, 0, sizeof(conf));
    size_t ssid_len = sizeof(conf.sta.ssid);
    size_t pass_len = sizeof(conf.sta.password);
    if (nvs_get_str(h, NVS_SSID, (char *)conf.sta.ssid, &ssid_len) != ESP_OK) {
        nvs_close(h);
        return false;
    }
    nvs_get_str(h, NVS_PASS, (char *)conf.sta.password, &pass_len); /* empty ok */
    nvs_close(h);

    conf.sta.threshold.authmode = WIFI_AUTH_WPA2_PSK;
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &conf));
    ESP_ERROR_CHECK(esp_wifi_start());
    return true;
}

bool wifi_manager_is_connected(void)
{
    return s_connected;
}
