#include "wire_state_strings.h"

const char *wire_arm_state_str(arm_state_t s)
{
    switch (s) {
        case ARM_STATE_ARMED: return "ARMED";
        case ARM_STATE_DISARMED: return "DISARMED";
        default: return "UNKNOWN";
    }
}

const char *wire_siren_state_str(siren_state_t s)
{
    switch (s) {
        case SIREN_STATE_OFF: return "OFF";
        case SIREN_STATE_ALARM: return "ALARM";
        case SIREN_STATE_TEST: return "TEST";
        default: return "UNKNOWN";
    }
}

const char *wire_health_state_str(health_state_t s)
{
    switch (s) {
        case HEALTH_STATE_HEALTHY: return "HEALTHY";
        case HEALTH_STATE_FAULT: return "FAULT";
        default: return "UNKNOWN";
    }
}
