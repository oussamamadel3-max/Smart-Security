/*
 * Experimental SNTP time sync (ESP-IDF esp_sntp). clockTrusted becomes true
 * only after a successful sync — never assumed. D-05 production NTP servers
 * remain an open decision; default pool is for lab builds only.
 */
#ifndef SNTP_SYNC_H
#define SNTP_SYNC_H

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

bool sntp_sync_start(const char *ntp_server_or_null);

bool sntp_sync_is_clock_trusted(void);

/* Milliseconds since Unix epoch when clockTrusted; else 0. */
int64_t sntp_sync_now_epoch_ms(void);

#ifdef __cplusplus
}
#endif

#endif /* SNTP_SYNC_H */
