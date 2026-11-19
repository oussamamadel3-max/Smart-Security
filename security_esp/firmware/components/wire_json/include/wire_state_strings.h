#ifndef WIRE_STATE_STRINGS_H
#define WIRE_STATE_STRINGS_H

#include "esp_core_types.h"

#ifdef __cplusplus
extern "C" {
#endif

const char *wire_arm_state_str(arm_state_t s);
const char *wire_siren_state_str(siren_state_t s);
const char *wire_health_state_str(health_state_t s);

#ifdef __cplusplus
}
#endif

#endif /* WIRE_STATE_STRINGS_H */
