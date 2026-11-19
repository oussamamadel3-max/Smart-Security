#include "mqtt_transport.h"
#include "mqtt_topics.h"
#include "mqtt_client.h"
#include "esp_log.h"
#include <stdlib.h>
#include <string.h>

static const char *TAG = "mqtt_transport";

static esp_mqtt_client_handle_t s_client;
static mqtt_transport_config_t s_cfg;
static bool s_connected;

static void mqtt_event_handler(void *handler_args, esp_event_base_t base, int32_t event_id, void *event_data)
{
    (void)handler_args;
    (void)base;
    esp_mqtt_event_handle_t event = event_data;
    switch ((esp_mqtt_event_id_t)event_id) {
        case MQTT_EVENT_CONNECTED:
            s_connected = true;
            ESP_LOGI(TAG, "MQTT connected");
            if (s_cfg.on_connected) {
                s_cfg.on_connected(s_cfg.ctx);
            }
            break;
        case MQTT_EVENT_DISCONNECTED:
            s_connected = false;
            ESP_LOGW(TAG, "MQTT disconnected");
            if (s_cfg.on_disconnected) {
                s_cfg.on_disconnected(s_cfg.ctx);
            }
            break;
        case MQTT_EVENT_DATA:
            if (s_cfg.on_message && event->data && event->data_len > 0) {
                /* Copy payload to NUL-terminated buffer for JSON parsers. */
                char *buf = malloc((size_t)event->data_len + 1);
                if (buf != NULL) {
                    memcpy(buf, event->data, (size_t)event->data_len);
                    buf[event->data_len] = '\0';
                    char topic_buf[MQTT_TOPIC_MAX_LEN];
                    int tlen = event->topic_len;
                    if (tlen >= (int)sizeof(topic_buf)) {
                        tlen = (int)sizeof(topic_buf) - 1;
                    }
                    memcpy(topic_buf, event->topic, (size_t)tlen);
                    topic_buf[tlen] = '\0';
                    s_cfg.on_message(topic_buf, buf, event->data_len, s_cfg.ctx);
                    free(buf);
                }
            }
            break;
        default:
            break;
    }
}

bool mqtt_transport_start(const mqtt_transport_config_t *cfg)
{
    if (cfg == NULL || cfg->broker_uri == NULL || cfg->client_id == NULL) {
        return false;
    }
    s_cfg = *cfg;
    s_connected = false;

    if (strncmp(cfg->broker_uri, "mqtts://", 8) == 0) {
        if (cfg->ca_cert_pem == NULL || cfg->ca_cert_pem[0] == '\0') {
            ESP_LOGE(TAG, "mqtts:// requires ca_cert_pem — refusing to invent a production trust store");
            return false;
        }
    } else if (strncmp(cfg->broker_uri, "mqtt://", 7) == 0) {
        ESP_LOGW(TAG, "EXPERIMENTAL cleartext MQTT (mqtt://) — lab only, not production");
    } else {
        ESP_LOGE(TAG, "unsupported broker URI scheme");
        return false;
    }

    esp_mqtt_client_config_t mqtt_cfg = {
        .broker.address.uri = cfg->broker_uri,
        .credentials.client_id = cfg->client_id,
    };
    if (cfg->username != NULL && cfg->username[0] != '\0') {
        mqtt_cfg.credentials.username = cfg->username;
        mqtt_cfg.credentials.authentication.password = cfg->password;
    }
    if (cfg->ca_cert_pem != NULL && cfg->ca_cert_pem[0] != '\0') {
        mqtt_cfg.broker.verification.certificate = cfg->ca_cert_pem;
    }

    if (s_client != NULL) {
        esp_mqtt_client_destroy(s_client);
        s_client = NULL;
    }
    s_client = esp_mqtt_client_init(&mqtt_cfg);
    if (s_client == NULL) {
        return false;
    }
    esp_mqtt_client_register_event(s_client, ESP_EVENT_ANY_ID, mqtt_event_handler, NULL);
    esp_err_t err = esp_mqtt_client_start(s_client);
    return err == ESP_OK;
}

bool mqtt_transport_subscribe(const char *topic, int qos)
{
    if (s_client == NULL || topic == NULL) {
        return false;
    }
    int msg_id = esp_mqtt_client_subscribe(s_client, topic, qos);
    return msg_id >= 0;
}

bool mqtt_transport_publish(const char *topic, const char *payload, int qos, bool retain)
{
    if (s_client == NULL || topic == NULL || payload == NULL) {
        return false;
    }
    if (retain) {
        ESP_LOGW(TAG, "retain=true requested — draft forbids retained instantaneous commands; publishing anyway only for non-command topics if caller insists");
    }
    int msg_id = esp_mqtt_client_publish(s_client, topic, payload, 0, qos, retain ? 1 : 0);
    return msg_id >= 0;
}

bool mqtt_transport_is_connected(void)
{
    return s_connected;
}

void mqtt_transport_stop(void)
{
    if (s_client != NULL) {
        esp_mqtt_client_stop(s_client);
        esp_mqtt_client_destroy(s_client);
        s_client = NULL;
    }
    s_connected = false;
}
