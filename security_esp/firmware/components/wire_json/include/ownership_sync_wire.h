#ifndef OWNERSHIP_SYNC_WIRE_H
#define OWNERSHIP_SYNC_WIRE_H

#include <stdint.h>
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
    uint64_t ownership_version;
    bool is_linked;
} ownership_sync_wire_t;

bool ownership_sync_parse(const char *json_text, ownership_sync_wire_t *out, char *err, size_t err_size);

#ifdef __cplusplus
}
#endif

#endif /* OWNERSHIP_SYNC_WIRE_H */
