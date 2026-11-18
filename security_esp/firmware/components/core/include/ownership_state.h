/*
 * T-07 — OWNERSHIP_SYNC handling. Draft wire shape (ownershipVersion,
 * isLinked) comes from the schema files under docs/contracts/mqtt/v1/,
 * explicitly marked "Not a G1-adopted production contract" there — field names are
 * reused here so the eventual real parser maps onto this struct without
 * renaming, but the numeric encoding/transport is still open.
 *
 * version/is_linked always reflect the latest OWNERSHIP_SYNC applied. The
 * device does not have a separate internal "confirm" delay: whatever this
 * struct currently holds is what gets reported as confirmedOwnershipVersion
 * on every outgoing STATE/COMMAND_ACK (see command_ack_builder.h). Applying a
 * sync never arms or disarms the device ("الإقرار لا يسلّح الجهاز").
 */
#ifndef OWNERSHIP_STATE_H
#define OWNERSHIP_STATE_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uint64_t version;
    bool is_linked;
} ownership_state_t;

void ownership_state_init(ownership_state_t *state);

/* Applies an incoming OWNERSHIP_SYNC. is_linked=false must still update
 * version/is_linked so the device can report it truthfully — it must not be
 * ignored just because the device is being unlinked. */
void ownership_state_apply_sync(ownership_state_t *state, uint64_t new_version, bool is_linked);

#ifdef __cplusplus
}
#endif

#endif /* OWNERSHIP_STATE_H */
