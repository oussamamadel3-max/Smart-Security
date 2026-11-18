/*
 * T-07/D-09 — build a COMMAND_ACK carrying the FULL actual device state after
 * processing a command (armState/sirenState/healthState/currentAlarmEventUid),
 * not just a thin "done" flag — D-09 is settled at the requirements level on
 * exactly this point. Field names mirror
 * docs/contracts/mqtt/v1/command-ack.schema.json (draft, "Not a G1-adopted
 * production contract"); JSON serialization itself is a transport (T-06)
 * concern, out of scope here.
 */
#ifndef COMMAND_ACK_BUILDER_H
#define COMMAND_ACK_BUILDER_H

#include "protection_core.h"
#include "ownership_state.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    arm_state_t arm_state;
    siren_state_t siren_state;
    health_state_t health_state;
    bool has_current_alarm_event_uid;
    uint32_t current_alarm_event_uid;
    uint64_t ownership_version;
    uint64_t confirmed_ownership_version; /* always == ownership_state.version; see ownership_state.h */
    bool clock_trusted;
    uint64_t result_state_sequence;
    bool executed; /* true -> status=EXECUTED, false -> status=REJECTED */
    const char *reject_code; /* NULL when executed == true */
} command_ack_t;

command_ack_t command_ack_build(const protection_core_t *core,
                                health_state_t health_state,
                                const ownership_state_t *ownership,
                                bool clock_trusted,
                                uint64_t result_state_sequence,
                                bool executed,
                                const char *reject_code);

#ifdef __cplusplus
}
#endif

#endif /* COMMAND_ACK_BUILDER_H */
