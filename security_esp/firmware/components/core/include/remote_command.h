/*
 * T-07 — remote-command admission checks: commandId de-dup, commandSequence
 * freshness, ownershipVersion match, and expiresAt validity gated on
 * clockTrusted. Pure logic: the caller already parsed EXECUTE_COMMAND into
 * this struct (JSON parsing is a transport/T-06 concern) and already knows
 * whether its clock is trusted (time sync is D-05/T-06 scope).
 *
 * Field names mirror docs/contracts/mqtt/v1/execute-command.schema.json
 * (draft, "Not a G1-adopted production contract").
 *
 * Check order is deliberate and safety-motivated:
 *   1. duplicate commandId -> DUPLICATE (no-op, resend cached ACK, never
 *      re-execute)
 *   2. ownershipVersion mismatch -> REJECT_OWNERSHIP (no execution before
 *      ownership sync — "لا تنفيذ قبل مزامنة إصدار الملكية")
 *   3. stale/old commandSequence -> REJECT_STALE_SEQUENCE
 *   4. clock not trusted -> REJECT_CLOCK_NOT_TRUSTED (cannot prove
 *      expiresAt, so refuse rather than guess — esp-context.md 2026-11-16)
 *   5. expired -> REJECT_EXPIRED
 */
#ifndef REMOTE_COMMAND_H
#define REMOTE_COMMAND_H

#include "esp_core_types.h"
#include "command_dedup.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    REMOTE_COMMAND_TYPE_ARM = 0,
    REMOTE_COMMAND_TYPE_DISARM,
    REMOTE_COMMAND_TYPE_MUTE_ALARM,
    REMOTE_COMMAND_TYPE_TEST_SIREN,
    REMOTE_COMMAND_TYPE_STOP_TEST
} remote_command_type_t;

typedef struct {
    char command_id[COMMAND_DEDUP_ID_MAX_LEN + 1];
    uint64_t command_sequence;
    uint64_t ownership_version;
    remote_command_type_t type;
    esp_core_time_ms_t expires_at_ms;
    bool has_target_event;
    uint32_t target_event_uid; /* required by schema when type == MUTE_ALARM */
} execute_command_t;

typedef struct {
    uint64_t last_applied_command_sequence;
    uint64_t current_ownership_version;
    bool clock_trusted;
    esp_core_time_ms_t now_ms;
} remote_command_context_t;

typedef enum {
    REMOTE_COMMAND_VERIFY_ACCEPT = 0,
    REMOTE_COMMAND_VERIFY_DUPLICATE,
    REMOTE_COMMAND_VERIFY_REJECT_OWNERSHIP_MISMATCH,
    REMOTE_COMMAND_VERIFY_REJECT_STALE_SEQUENCE,
    REMOTE_COMMAND_VERIFY_REJECT_CLOCK_NOT_TRUSTED,
    REMOTE_COMMAND_VERIFY_REJECT_EXPIRED
} remote_command_verify_result_t;

remote_command_verify_result_t remote_command_verify(const execute_command_t *cmd,
                                                      const remote_command_context_t *ctx,
                                                      command_dedup_t *dedup);

#ifdef __cplusplus
}
#endif

#endif /* REMOTE_COMMAND_H */
