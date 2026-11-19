/*
 * Pure connection lifecycle + reconnect backoff for the experimental network
 * stack (ESP-C-04 gap closure). No Wi-Fi/MQTT calls here — only state and
 * timing math so host tests can cover transitions without ESP-IDF.
 *
 * Local protection must keep running regardless of this state machine.
 */
#ifndef CONNECTION_LIFECYCLE_H
#define CONNECTION_LIFECYCLE_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    CONN_LIFECYCLE_DISCONNECTED = 0,
    CONN_LIFECYCLE_WIFI_CONNECTING,
    CONN_LIFECYCLE_WIFI_CONNECTED,
    CONN_LIFECYCLE_TIME_SYNCING,
    CONN_LIFECYCLE_MQTT_CONNECTING,
    CONN_LIFECYCLE_CONNECTED,
    CONN_LIFECYCLE_RECONNECTING
} conn_lifecycle_state_t;

typedef struct {
    conn_lifecycle_state_t state;
    uint32_t reconnect_attempt;
    uint32_t base_backoff_ms;
    uint32_t max_backoff_ms;
} conn_lifecycle_t;

void conn_lifecycle_init(conn_lifecycle_t *life, uint32_t base_backoff_ms, uint32_t max_backoff_ms);

void conn_lifecycle_begin_wifi(conn_lifecycle_t *life);
void conn_lifecycle_on_wifi_connected(conn_lifecycle_t *life);
void conn_lifecycle_begin_time_sync(conn_lifecycle_t *life);
void conn_lifecycle_on_time_sync_done(conn_lifecycle_t *life); /* may leave clockTrusted false */
void conn_lifecycle_begin_mqtt(conn_lifecycle_t *life);
void conn_lifecycle_on_mqtt_connected(conn_lifecycle_t *life);
void conn_lifecycle_on_transport_lost(conn_lifecycle_t *life);

/* Exponential backoff capped at max_backoff_ms. Resets when CONNECTED. */
uint32_t conn_lifecycle_next_backoff_ms(const conn_lifecycle_t *life);

bool conn_lifecycle_is_mqtt_ready(const conn_lifecycle_t *life);

#ifdef __cplusplus
}
#endif

#endif /* CONNECTION_LIFECYCLE_H */
