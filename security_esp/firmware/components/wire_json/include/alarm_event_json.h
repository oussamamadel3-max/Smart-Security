#ifndef ALARM_EVENT_JSON_H
#define ALARM_EVENT_JSON_H

#include "uuid128.h"
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uuid128_t event_uid;
    uint64_t ownership_version;
    const char *sensor_key; /* e.g. "vibration-1" — draft sample name */
    bool was_buffered;
    bool clock_trusted;
    bool alarm_phase_ended; /* true -> ENDED, false -> ACTIVE (or omit) */
    bool include_alarm_phase; /* when false, omit field (legacy ACTIVE default) */
} alarm_event_wire_t;

char *alarm_event_to_json(const alarm_event_wire_t *ev,
                          const char *device_id,
                          const char *message_id,
                          const char *boot_id);

#ifdef __cplusplus
}
#endif

#endif /* ALARM_EVENT_JSON_H */
