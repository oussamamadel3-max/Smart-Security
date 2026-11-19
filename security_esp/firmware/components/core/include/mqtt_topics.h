/*
 * Draft MQTT topic helpers for docs/contracts/mqtt/v1/README.md topic table.
 * Experimental only — Not a G1-adopted production contract. Pure C, no ESP-IDF.
 */
#ifndef MQTT_TOPICS_H
#define MQTT_TOPICS_H

#include <stddef.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/* devices/{deviceId}/… — deviceId must be the 36-char UUID string. */
#define MQTT_TOPIC_MAX_LEN 96

bool mqtt_topic_state(const char *device_id, char *out, size_t out_size);
bool mqtt_topic_events(const char *device_id, char *out, size_t out_size);
bool mqtt_topic_acks(const char *device_id, char *out, size_t out_size);
bool mqtt_topic_commands(const char *device_id, char *out, size_t out_size);

#ifdef __cplusplus
}
#endif

#endif /* MQTT_TOPICS_H */
