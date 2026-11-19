#ifndef HEARTBEAT_JSON_H
#define HEARTBEAT_JSON_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

char *heartbeat_to_json(const char *device_id,
                        const char *message_id,
                        const char *boot_id,
                        bool clock_trusted);

#ifdef __cplusplus
}
#endif

#endif /* HEARTBEAT_JSON_H */
