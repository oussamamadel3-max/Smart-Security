/*
 * T-07 — "prevent duplicate execution". commandSequence rejects anything
 * older than the last applied command, but a *retransmission of the same*
 * command (same commandId) after a network blip — before the device's ACK
 * ever reached the backend — would otherwise look like a fresh in-order
 * command and get executed twice (e.g. two TEST_SIREN starts). This module
 * remembers a small ring of recently-seen commandId strings so such a replay
 * is recognised instead of re-executed.
 *
 * This is a bounded, best-effort window (capacity commands), not a durable
 * ledger — matches "منع التنفيذ المكرر" as a protection against
 * retransmission, not a claim of exactly-once delivery across a reboot.
 */
#ifndef COMMAND_DEDUP_H
#define COMMAND_DEDUP_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

#define COMMAND_DEDUP_CAPACITY 8
#define COMMAND_DEDUP_ID_MAX_LEN 36 /* uuid string length, no NUL padding assumed beyond buffer size */

typedef struct {
    char ids[COMMAND_DEDUP_CAPACITY][COMMAND_DEDUP_ID_MAX_LEN + 1];
    bool slot_filled[COMMAND_DEDUP_CAPACITY];
    uint8_t next_slot; /* ring write cursor */
} command_dedup_t;

void command_dedup_init(command_dedup_t *dedup);

/* Returns true if command_id was already recorded (duplicate — do not
 * execute again). Returns false and records it if this is the first time
 * seen. command_id must be a NUL-terminated string of at most
 * COMMAND_DEDUP_ID_MAX_LEN characters. */
bool command_dedup_check_and_record(command_dedup_t *dedup, const char *command_id);

#ifdef __cplusplus
}
#endif

#endif /* COMMAND_DEDUP_H */
