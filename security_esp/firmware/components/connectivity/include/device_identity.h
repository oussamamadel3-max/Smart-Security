/*
 * Device identity (deviceId UUID + bootId) stored in NVS for experimental
 * MQTT draft use. No production auth scheme is invented here — MQTT
 * username/password/cert are optional lab placeholders loaded from NVS
 * when present, never hard-coded.
 */
#ifndef DEVICE_IDENTITY_H
#define DEVICE_IDENTITY_H

#include "uuid128.h"
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

#define DEVICE_ID_STR_LEN 36
#define BOOT_ID_MAX_LEN 64

typedef struct {
    char device_id[DEVICE_ID_STR_LEN + 1];
    char boot_id[BOOT_ID_MAX_LEN + 1];
} device_identity_t;

/* Loads deviceId from NVS namespace "dev_id"; if missing, generates a UUIDv4
 * via the supplied RNG and persists it. Always regenerates boot_id for this
 * boot (not persisted). Returns false on NVS failure. */
bool device_identity_init(device_identity_t *out,
                          void (*rng_fill)(void *ctx, uint8_t *buf, size_t len),
                          void *rng_ctx);

#ifdef __cplusplus
}
#endif

#endif /* DEVICE_IDENTITY_H */
