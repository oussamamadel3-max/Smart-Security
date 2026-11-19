/*
 * ESP-C-03 — real hardware RNG source for core/uuid128.h's injected-
 * randomness design. The canonical home for this wrapper is here
 * (components/port_esp32), not main/app_main.c, so any future consumer of
 * uuid128_generate_v4() under ESP-IDF reuses this instead of re-wrapping
 * esp_fill_random() ad hoc.
 */
#ifndef HW_RNG_H
#define HW_RNG_H

#include "uuid128.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Matches uuid128_random_source_fn_t. ctx is unused (pass NULL). */
void hw_rng_fill(void *ctx, uint8_t *out, size_t len);

#ifdef __cplusplus
}
#endif

#endif /* HW_RNG_H */
