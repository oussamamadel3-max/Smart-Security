#ifndef EVENT_LOSS_JSON_H
#define EVENT_LOSS_JSON_H

#include "uuid128.h"
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

char *event_loss_to_json(const char *device_id,
                         const char *message_id,
                         const char *boot_id,
                         const uuid128_t *report_uid,
                         uint64_t ownership_version,
                         bool clock_trusted);

#ifdef __cplusplus
}
#endif

#endif /* EVENT_LOSS_JSON_H */
