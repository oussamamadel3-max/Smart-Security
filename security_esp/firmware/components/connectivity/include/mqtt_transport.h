/*
 * Experimental MQTT client over ESP-IDF esp_mqtt. Uses draft topics from
 * docs/contracts/mqtt/v1/README.md. TLS is optional:
 *   - mqtt://  → cleartext lab broker only (logged as EXPERIMENTAL)
 *   - mqtts:// → requires a CA PEM configured via NVS/config; no production
 *                credential scheme is invented here (D-04 / G1 open).
 */
#ifndef MQTT_TRANSPORT_H
#define MQTT_TRANSPORT_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*mqtt_transport_message_cb_t)(const char *topic, const char *payload, int payload_len, void *ctx);
typedef void (*mqtt_transport_connected_cb_t)(void *ctx);
typedef void (*mqtt_transport_disconnected_cb_t)(void *ctx);

typedef struct {
    const char *broker_uri;     /* e.g. mqtt://192.168.1.10:1883 — from NVS/config, not hard-coded secrets */
    const char *client_id;      /* deviceId */
    const char *username;       /* optional lab placeholder; may be NULL */
    const char *password;       /* optional lab placeholder; may be NULL — never log */
    const char *ca_cert_pem;    /* required for mqtts://; NULL for mqtt:// */
    mqtt_transport_message_cb_t on_message;
    mqtt_transport_connected_cb_t on_connected;
    mqtt_transport_disconnected_cb_t on_disconnected;
    void *ctx;
} mqtt_transport_config_t;

bool mqtt_transport_start(const mqtt_transport_config_t *cfg);

bool mqtt_transport_subscribe(const char *topic, int qos);

bool mqtt_transport_publish(const char *topic, const char *payload, int qos, bool retain);

bool mqtt_transport_is_connected(void);

void mqtt_transport_stop(void);

#ifdef __cplusplus
}
#endif

#endif /* MQTT_TRANSPORT_H */
