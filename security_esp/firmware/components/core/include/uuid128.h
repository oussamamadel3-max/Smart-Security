/*
 * ESP-C-05 follow-up — real UUID support for local alarm-event identity.
 *
 * Why this exists: the backend's authoritative, already-implemented schema
 * (security_backEnd/Domain/Entities/Events/AlarmEvent.cs: `public Guid
 * EventUid`) and the draft MQTT contract (schema files under
 * docs/contracts/mqtt/v1/, `currentAlarmEventUid`/`targetAlarmEventUid`)
 * both require a real UUID.
 * Earlier in this project, local alarm events used a `uint32_t` monotonic
 * counter instead (flagged as an open gap). This is not an open product
 * decision — the backend already committed to `Guid` — so the fix is to
 * generate real UUIDs here, not to keep waiting.
 *
 * Randomness is injected (uuid128_random_source_fn_t), not read from a
 * hardware RNG directly, so this stays host-testable without ESP-IDF:
 *   - Production (components/port_esp32, not written yet) wires this to
 *     `esp_fill_random()`.
 *   - Host tests pass a small deterministic fake so assertions are
 *     reproducible (see tests/host/src/test_uuid128.c).
 */
#ifndef UUID128_H
#define UUID128_H

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    uint8_t bytes[16];
} uuid128_t;

typedef void (*uuid128_random_source_fn_t)(void *ctx, uint8_t *out, size_t len);

/* Fills `out` with `len` random bytes via `source`, then sets the version
 * (4) and variant (RFC 4122) bits — standard UUIDv4 construction. */
uuid128_t uuid128_generate_v4(uuid128_random_source_fn_t source, void *source_ctx);

/* All-zero sentinel, used as "no uuid" (distinct from a real, if
 * astronomically-unlikely-to-occur, all-zero random UUID — callers that
 * need "no active event" should rely on alarm_event_id_t.is_active, not on
 * this check alone). */
bool uuid128_is_nil(const uuid128_t *u);

bool uuid128_equal(const uuid128_t *a, const uuid128_t *b);

/* Writes the canonical lowercase "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
 * form (36 chars + NUL) into out, which must be at least 37 bytes. */
void uuid128_to_string(const uuid128_t *u, char out[37]);

/* Parses a UUID string matching ^[0-9a-fA-F]{8}-...{4}-...{4}-...{4}-...{12}$
 * (the pattern the schema files under docs/contracts/mqtt/v1/ use —
 * case-insensitive, dashes required at the standard positions). Returns false on any other
 * shape, including missing/extra characters or a missing dash. */
bool uuid128_from_string(const char *text, uuid128_t *out);

#ifdef __cplusplus
}
#endif

#endif /* UUID128_H */
