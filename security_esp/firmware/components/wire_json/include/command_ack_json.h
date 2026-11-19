/*
 * ESP-C-04 — builds a COMMAND_ACK JSON string from command_ack_builder.h's
 * command_ack_t, per the draft schema
 * docs/contracts/mqtt/v1/command-ack.schema.json ("Not a G1-adopted
 * production contract").
 *
 * currentAlarmEventUid is emitted as the real UUID string (uuid128_t, see
 * esp_core_types.h) when command_ack_t.has_current_alarm_event_uid is true,
 * and as JSON null otherwise — matching the schema's `anyOf: [uuid, null]`.
 */
#ifndef COMMAND_ACK_JSON_H
#define COMMAND_ACK_JSON_H

#include "command_ack_builder.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Returns a heap-allocated, NUL-terminated JSON string the caller must
 * free() (cJSON_Print semantics), or NULL on allocation failure.
 * device_id/message_id/boot_id/command_id are passed through verbatim —
 * this module does not validate or generate them. */
char *command_ack_to_json(const command_ack_t *ack,
                          const char *device_id,
                          const char *message_id,
                          const char *boot_id,
                          const char *command_id);

#ifdef __cplusplus
}
#endif

#endif /* COMMAND_ACK_JSON_H */
