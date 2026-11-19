#include "mqtt_topics.h"
#include <stdio.h>
#include <string.h>

static bool build_topic(const char *device_id, const char *suffix, char *out, size_t out_size)
{
    if (device_id == NULL || suffix == NULL || out == NULL || out_size == 0) {
        return false;
    }
    if (strlen(device_id) != 36) {
        return false;
    }
    int n = snprintf(out, out_size, "devices/%s/%s", device_id, suffix);
    return n > 0 && (size_t)n < out_size;
}

bool mqtt_topic_state(const char *device_id, char *out, size_t out_size)
{
    return build_topic(device_id, "state", out, out_size);
}

bool mqtt_topic_events(const char *device_id, char *out, size_t out_size)
{
    return build_topic(device_id, "events", out, out_size);
}

bool mqtt_topic_acks(const char *device_id, char *out, size_t out_size)
{
    return build_topic(device_id, "acks", out, out_size);
}

bool mqtt_topic_commands(const char *device_id, char *out, size_t out_size)
{
    return build_topic(device_id, "commands", out, out_size);
}
