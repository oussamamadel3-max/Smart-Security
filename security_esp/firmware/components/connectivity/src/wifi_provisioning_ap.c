#include "wifi_provisioning_ap.h"
#include "wifi_manager.h"
#include "esp_wifi.h"
#include "esp_http_server.h"
#include "esp_log.h"
#include "esp_timer.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static const char *TAG = "wifi_prov_ap";

static httpd_handle_t s_server;
static esp_timer_handle_t s_timeout_timer;
static wifi_provisioning_ap_config_t s_cfg;
static bool s_active;

/* Reject control verbs explicitly — provisioning must never arm/disarm. */
static bool looks_like_protection_control(const char *body)
{
    if (body == NULL) {
        return false;
    }
    return strstr(body, "ARM") != NULL
        || strstr(body, "DISARM") != NULL
        || strstr(body, "MUTE") != NULL
        || strstr(body, "TEST_SIREN") != NULL
        || strstr(body, "STOP_TEST") != NULL;
}

static esp_err_t root_get_handler(httpd_req_t *req)
{
    static const char *page =
        "<!doctype html><html><body>"
        "<h1>SmartSecurity Wi-Fi Setup (experimental)</h1>"
        "<p>Provisioning only — protection commands are rejected.</p>"
        "<form method='POST' action='/wifi'>"
        "SSID: <input name='ssid'/><br/>"
        "Password: <input name='pass' type='password'/><br/>"
        "<button type='submit'>Save</button></form>"
        "</body></html>";
    httpd_resp_set_type(req, "text/html");
    return httpd_resp_send(req, page, HTTPD_RESP_USE_STRLEN);
}

static bool extract_form_field(const char *body, const char *key, char *out, size_t out_size)
{
    char pattern[32];
    snprintf(pattern, sizeof(pattern), "%s=", key);
    const char *p = strstr(body, pattern);
    if (p == NULL) {
        return false;
    }
    p += strlen(pattern);
    size_t i = 0;
    while (*p && *p != '&' && i + 1 < out_size) {
        if (*p == '+') {
            out[i++] = ' ';
            p++;
        } else if (*p == '%' && p[1] && p[2]) {
            /* Minimal URL-decode for hex pairs; keep simple for lab form. */
            char hex[3] = { p[1], p[2], 0 };
            out[i++] = (char)strtol(hex, NULL, 16);
            p += 3;
        } else {
            out[i++] = *p++;
        }
    }
    out[i] = '\0';
    return i > 0;
}

static esp_err_t wifi_post_handler(httpd_req_t *req)
{
    int total = req->content_len;
    if (total <= 0 || total > 512) {
        httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "bad body");
        return ESP_FAIL;
    }
    char *body = calloc(1, (size_t)total + 1);
    if (body == NULL) {
        return ESP_FAIL;
    }
    int received = httpd_req_recv(req, body, total);
    if (received <= 0) {
        free(body);
        return ESP_FAIL;
    }
    body[received] = '\0';

    if (looks_like_protection_control(body)) {
        ESP_LOGW(TAG, "rejected protection control attempt on provisioning path");
        free(body);
        httpd_resp_send_err(req, HTTPD_403_FORBIDDEN, "protection commands forbidden on provisioning AP");
        return ESP_FAIL;
    }

    char ssid[33];
    char pass[64];
    ssid[0] = pass[0] = '\0';
    if (!extract_form_field(body, "ssid", ssid, sizeof(ssid))) {
        free(body);
        httpd_resp_send_err(req, HTTPD_400_BAD_REQUEST, "ssid required");
        return ESP_FAIL;
    }
    extract_form_field(body, "pass", pass, sizeof(pass));
    free(body);

    bool ok = wifi_manager_save_credentials(ssid, pass);
    /* Never log password. */
    ESP_LOGI(TAG, "provisioning save ssid_len=%u ok=%d", (unsigned)strlen(ssid), (int)ok);
    httpd_resp_sendstr(req, ok ? "saved" : "save failed");

    if (s_cfg.on_done) {
        s_cfg.on_done(ok, s_cfg.ctx);
    }
    wifi_provisioning_ap_stop();
    return ESP_OK;
}

static void on_timeout(void *arg)
{
    (void)arg;
    ESP_LOGW(TAG, "provisioning timeout — closing AP");
    if (s_cfg.on_done) {
        s_cfg.on_done(false, s_cfg.ctx);
    }
    wifi_provisioning_ap_stop();
}

bool wifi_provisioning_ap_start(const wifi_provisioning_ap_config_t *cfg)
{
    if (cfg == NULL || cfg->ap_ssid == NULL || s_active) {
        return false;
    }
    s_cfg = *cfg;

    esp_netif_create_default_wifi_ap();
    wifi_config_t ap_conf;
    memset(&ap_conf, 0, sizeof(ap_conf));
    strncpy((char *)ap_conf.ap.ssid, cfg->ap_ssid, sizeof(ap_conf.ap.ssid) - 1);
    ap_conf.ap.ssid_len = strlen(cfg->ap_ssid);
    ap_conf.ap.channel = 1;
    ap_conf.ap.max_connection = 2;
    ap_conf.ap.authmode = WIFI_AUTH_OPEN; /* open AP for lab setup only */

    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_APSTA));
    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_AP, &ap_conf));
    ESP_ERROR_CHECK(esp_wifi_start());

    httpd_config_t http_cfg = HTTPD_DEFAULT_CONFIG();
    http_cfg.server_port = 80;
    if (httpd_start(&s_server, &http_cfg) != ESP_OK) {
        return false;
    }
    httpd_uri_t root = { .uri = "/", .method = HTTP_GET, .handler = root_get_handler };
    httpd_uri_t wifi = { .uri = "/wifi", .method = HTTP_POST, .handler = wifi_post_handler };
    httpd_register_uri_handler(s_server, &root);
    httpd_register_uri_handler(s_server, &wifi);

    if (cfg->timeout_ms > 0) {
        const esp_timer_create_args_t targs = {
            .callback = &on_timeout,
            .name = "wifi_prov_timeout",
        };
        ESP_ERROR_CHECK(esp_timer_create(&targs, &s_timeout_timer));
        ESP_ERROR_CHECK(esp_timer_start_once(s_timeout_timer, (uint64_t)cfg->timeout_ms * 1000ULL));
    }

    s_active = true;
    ESP_LOGI(TAG, "SoftAP provisioning started (ssid=%s)", cfg->ap_ssid);
    return true;
}

void wifi_provisioning_ap_stop(void)
{
    if (s_timeout_timer) {
        esp_timer_stop(s_timeout_timer);
        esp_timer_delete(s_timeout_timer);
        s_timeout_timer = NULL;
    }
    if (s_server) {
        httpd_stop(s_server);
        s_server = NULL;
    }
    s_active = false;
    ESP_LOGI(TAG, "SoftAP provisioning stopped");
}

bool wifi_provisioning_ap_is_active(void)
{
    return s_active;
}
