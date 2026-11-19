#include "sntp_sync.h"
#include "esp_log.h"
#include "esp_sntp.h"
#include "esp_idf_version.h"
#include <time.h>
#include <sys/time.h>

static const char *TAG = "sntp_sync";
static volatile bool s_trusted;

static void on_sync(struct timeval *tv)
{
    (void)tv;
    s_trusted = true;
    ESP_LOGI(TAG, "SNTP sync OK — clockTrusted=true");
}

bool sntp_sync_start(const char *ntp_server_or_null)
{
    s_trusted = false;
    const char *server = (ntp_server_or_null != NULL && ntp_server_or_null[0] != '\0')
                             ? ntp_server_or_null
                             : "pool.ntp.org"; /* lab default only — D-05 open */
#if ESP_IDF_VERSION >= ESP_IDF_VERSION_VAL(5, 0, 0)
    esp_sntp_setoperatingmode(SNTP_OPMODE_POLL);
    esp_sntp_setservername(0, server);
    esp_sntp_set_time_sync_notification_cb(on_sync);
    esp_sntp_init();
#else
    sntp_setoperatingmode(SNTP_OPMODE_POLL);
    sntp_setservername(0, (char *)server);
    sntp_set_time_sync_notification_cb(on_sync);
    sntp_init();
#endif
    ESP_LOGI(TAG, "SNTP started (server=%s) — experimental", server);
    return true;
}

bool sntp_sync_is_clock_trusted(void)
{
    return s_trusted;
}

int64_t sntp_sync_now_epoch_ms(void)
{
    if (!s_trusted) {
        return 0;
    }
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return ((int64_t)tv.tv_sec * 1000LL) + ((int64_t)tv.tv_usec / 1000LL);
}
