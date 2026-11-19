/*
 * ESP-C-04 — EXECUTE_COMMAND draft-contract JSON parsing (experimental, NOT
 * G1-adopted; see docs/contracts/mqtt/v1/execute-command.schema.json, which
 * says exactly that). This is a wire-level struct, deliberately separate
 * from remote_command.h's execute_command_t: device_id/message_id stay as
 * strings here (transport-level identity, not something the pure
 * command-verification logic needs), but UUID-shaped payload fields
 * (commandId, targetAlarmEventUid) carry over as real uuid128_t values now
 * — see esp_core_types.h / uuid128.h for why this used to be a uint32_t
 * local counter and is not anymore.
 *
 * device_id/message_id/command_id are kept as both the validated raw
 * string (here) and, for command_id, copied into execute_command_t as a
 * string (used for de-dup, which works on opaque strings) by
 * execute_command_wire_to_core().
 */
#ifndef EXECUTE_COMMAND_WIRE_H
#define EXECUTE_COMMAND_WIRE_H

#include "remote_command.h"

#ifdef __cplusplus
extern "C" {
#endif

#define WIRE_UUID_LEN 36

typedef struct {
    char device_id[WIRE_UUID_LEN + 1];
    char message_id[WIRE_UUID_LEN + 1];
    char command_id[WIRE_UUID_LEN + 1];
    uint64_t command_sequence;
    uint64_t ownership_version;
    remote_command_type_t type;
    int64_t expires_at_epoch_ms;
    bool has_target_event;
    uuid128_t target_alarm_event_uid;
} execute_command_wire_t;

/* Parses one EXECUTE_COMMAND JSON message. Returns false and writes a short
 * human-readable reason into err (if err != NULL) on any schema violation:
 * missing/wrong-type required field, unknown `type`, malformed UUID, bad
 * `expiresAt`, or a MUTE_ALARM command missing `targetAlarmEventUid`. Does
 * NOT reject on unknown extra fields (the schema's additionalProperties is
 * true) or on schemaVersion mismatch being anything other than exactly 1 is
 * still rejected, since the schema pins it to `const: 1`. */
bool execute_command_parse(const char *json_text, execute_command_wire_t *out, char *err, size_t err_size);

/* Fills `core_cmd` from `wire` for everything remote_command_verify() and
 * protection_core_mute() need. Every field, including target_event_uid, now
 * carries over faithfully. */
void execute_command_wire_to_core(const execute_command_wire_t *wire, execute_command_t *core_cmd);

#ifdef __cplusplus
}
#endif

#endif /* EXECUTE_COMMAND_WIRE_H */
