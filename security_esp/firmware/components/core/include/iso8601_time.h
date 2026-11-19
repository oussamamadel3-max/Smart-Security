/*
 * ESP-C-04 — parses the one fixed ISO-8601 UTC format the draft contract
 * actually emits for `expiresAt`: "yyyy-MM-ddTHH:mm:ssZ", no fractional
 * seconds, always 'Z' (docs/contracts/mqtt/v1/execute-command.schema.json).
 * Not a general ISO-8601 parser — rejects anything that doesn't match this
 * exact shape rather than guessing.
 *
 * Output is epoch milliseconds (UTC, 1970-01-01T00:00:00Z = 0), using the
 * civil_from_days algorithm (public-domain, Howard Hinnant) so no libc
 * timezone/DST-dependent function (timegm, mktime) is needed — those behave
 * inconsistently across the host toolchain used here and the ESP-IDF/newlib
 * target this will eventually run under.
 *
 * Caller responsibility (not this module's job): whatever "now" value is
 * compared against this result (see remote_command.h's `now_ms`) must be in
 * the SAME UTC-epoch-ms timebase, not a boot-relative monotonic clock. That
 * mapping belongs to the time-sync layer (D-05), not written yet.
 */
#ifndef ISO8601_TIME_H
#define ISO8601_TIME_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/* Returns false (and leaves *out_epoch_ms untouched) if `text` is not
 * exactly "yyyy-MM-ddTHH:mm:ssZ" or encodes an impossible calendar date. */
bool iso8601_utc_to_epoch_ms(const char *text, int64_t *out_epoch_ms);

#ifdef __cplusplus
}
#endif

#endif /* ISO8601_TIME_H */
