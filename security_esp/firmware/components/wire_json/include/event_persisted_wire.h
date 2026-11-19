#ifndef EVENT_PERSISTED_WIRE_H
#define EVENT_PERSISTED_WIRE_H

#include "uuid128.h"
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifndef WIRE_UUID_LEN
#define WIRE_UUID_LEN 36
#endif

typedef struct {
    char device_id[WIRE_UUID_LEN + 1];
    char message_id[WIRE_UUID_LEN + 1];
    uuid128_t event_uid;
    bool stored;              /* must be true per draft schema */
    bool has_alarm_phase;     /* false = legacy omit -> ACTIVE */
    bool alarm_phase_ended;   /* true = ENDED */
} event_persisted_wire_t;

bool event_persisted_parse(const char *json_text, event_persisted_wire_t *out, char *err, size_t err_size);

#ifdef __cplusplus
}
#endif

#endif /* EVENT_PERSISTED_WIRE_H */
