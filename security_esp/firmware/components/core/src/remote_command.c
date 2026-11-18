#include "remote_command.h"

remote_command_verify_result_t remote_command_verify(const execute_command_t *cmd,
                                                      const remote_command_context_t *ctx,
                                                      command_dedup_t *dedup)
{
    if (command_dedup_check_and_record(dedup, cmd->command_id)) {
        return REMOTE_COMMAND_VERIFY_DUPLICATE;
    }
    if (cmd->ownership_version != ctx->current_ownership_version) {
        return REMOTE_COMMAND_VERIFY_REJECT_OWNERSHIP_MISMATCH;
    }
    if (cmd->command_sequence <= ctx->last_applied_command_sequence) {
        return REMOTE_COMMAND_VERIFY_REJECT_STALE_SEQUENCE;
    }
    if (!ctx->clock_trusted) {
        return REMOTE_COMMAND_VERIFY_REJECT_CLOCK_NOT_TRUSTED;
    }
    if (cmd->expires_at_ms <= ctx->now_ms) {
        return REMOTE_COMMAND_VERIFY_REJECT_EXPIRED;
    }
    return REMOTE_COMMAND_VERIFY_ACCEPT;
}
