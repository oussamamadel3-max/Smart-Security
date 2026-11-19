#include "connection_lifecycle.h"

void conn_lifecycle_init(conn_lifecycle_t *life, uint32_t base_backoff_ms, uint32_t max_backoff_ms)
{
    life->state = CONN_LIFECYCLE_DISCONNECTED;
    life->reconnect_attempt = 0;
    life->base_backoff_ms = (base_backoff_ms == 0) ? 1000u : base_backoff_ms;
    life->max_backoff_ms = (max_backoff_ms < life->base_backoff_ms) ? life->base_backoff_ms : max_backoff_ms;
}

void conn_lifecycle_begin_wifi(conn_lifecycle_t *life)
{
    life->state = (life->reconnect_attempt > 0) ? CONN_LIFECYCLE_RECONNECTING : CONN_LIFECYCLE_WIFI_CONNECTING;
}

void conn_lifecycle_on_wifi_connected(conn_lifecycle_t *life)
{
    life->state = CONN_LIFECYCLE_WIFI_CONNECTED;
}

void conn_lifecycle_begin_time_sync(conn_lifecycle_t *life)
{
    life->state = CONN_LIFECYCLE_TIME_SYNCING;
}

void conn_lifecycle_on_time_sync_done(conn_lifecycle_t *life)
{
    /* Time sync failure still allows MQTT connect; clockTrusted stays false. */
    life->state = CONN_LIFECYCLE_WIFI_CONNECTED;
}

void conn_lifecycle_begin_mqtt(conn_lifecycle_t *life)
{
    life->state = CONN_LIFECYCLE_MQTT_CONNECTING;
}

void conn_lifecycle_on_mqtt_connected(conn_lifecycle_t *life)
{
    life->state = CONN_LIFECYCLE_CONNECTED;
    life->reconnect_attempt = 0;
}

void conn_lifecycle_on_transport_lost(conn_lifecycle_t *life)
{
    if (life->reconnect_attempt < UINT32_MAX) {
        life->reconnect_attempt++;
    }
    life->state = CONN_LIFECYCLE_RECONNECTING;
}

uint32_t conn_lifecycle_next_backoff_ms(const conn_lifecycle_t *life)
{
    uint32_t attempt = life->reconnect_attempt;
    if (attempt == 0) {
        return life->base_backoff_ms;
    }
    uint32_t delay = life->base_backoff_ms;
    for (uint32_t i = 1; i < attempt; i++) {
        if (delay > life->max_backoff_ms / 2u) {
            return life->max_backoff_ms;
        }
        delay *= 2u;
    }
    return (delay > life->max_backoff_ms) ? life->max_backoff_ms : delay;
}

bool conn_lifecycle_is_mqtt_ready(const conn_lifecycle_t *life)
{
    return life->state == CONN_LIFECYCLE_CONNECTED;
}
