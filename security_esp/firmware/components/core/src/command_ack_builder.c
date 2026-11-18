#include "command_ack_builder.h"

command_ack_t command_ack_build(const protection_core_t *core,
                                health_state_t health_state,
                                const ownership_state_t *ownership,
                                bool clock_trusted,
                                uint64_t result_state_sequence,
                                bool executed,
                                const char *reject_code)
{
    command_ack_t ack;
    ack.arm_state = core->arm_state;
    ack.siren_state = protection_core_siren_state(core);
    ack.health_state = health_state;
    ack.has_current_alarm_event_uid = core->current_event.is_active;
    ack.current_alarm_event_uid = core->current_event.uid;
    ack.ownership_version = ownership->version;
    ack.confirmed_ownership_version = ownership->version;
    ack.clock_trusted = clock_trusted;
    ack.result_state_sequence = result_state_sequence;
    ack.executed = executed;
    ack.reject_code = reject_code;
    return ack;
}
