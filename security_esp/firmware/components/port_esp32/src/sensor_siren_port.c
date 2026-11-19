#include "sensor_siren_port.h"
#include "driver/gpio.h"
#include "esp_timer.h"
#include "esp_log.h"
#include <string.h>

static const char *TAG = "sensor_siren_port";

typedef struct {
    sensor_siren_port_config_t config;
    protection_core_t *core;
    bool last_sensor_active;
} port_state_t;

static port_state_t s_state;
static esp_timer_handle_t s_timer;

static inline esp_core_time_ms_t now_ms(void)
{
    return (esp_core_time_ms_t)(esp_timer_get_time() / 1000);
}

static bool read_sensor_active(const sensor_siren_port_config_t *cfg)
{
    int level = gpio_get_level((gpio_num_t)cfg->sensor_gpio);
    bool raw_high = (level != 0);
    return cfg->invert_sensor_level ? !raw_high : raw_high;
}

static void write_siren(const sensor_siren_port_config_t *cfg, bool on)
{
    int level = cfg->invert_siren_level ? (on ? 0 : 1) : (on ? 1 : 0);
    gpio_set_level((gpio_num_t)cfg->siren_gpio, level);
}

static void poll_timer_cb(void *arg)
{
    (void)arg;
    esp_core_time_ms_t t = now_ms();

    bool active = read_sensor_active(&s_state.config);
    if (active && !s_state.last_sensor_active) {
        /* Rising edge (in the logical, post-inversion sense) = one pulse. */
        protection_core_on_sensor_pulse(s_state.core, t);
    }
    s_state.last_sensor_active = active;

    protection_core_tick(s_state.core, t);

    siren_state_t siren = protection_core_siren_state(s_state.core);
    write_siren(&s_state.config, siren == SIREN_STATE_ALARM || siren == SIREN_STATE_TEST);
}

bool sensor_siren_port_start(const sensor_siren_port_config_t *config, protection_core_t *core)
{
    if (config == NULL || core == NULL || config->poll_interval_ms == 0) {
        return false;
    }
    memset(&s_state, 0, sizeof(s_state));
    s_state.config = *config;
    s_state.core = core;

    gpio_config_t sensor_cfg = {
        .pin_bit_mask = 1ULL << config->sensor_gpio,
        .mode = GPIO_MODE_INPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    if (gpio_config(&sensor_cfg) != ESP_OK) {
        ESP_LOGE(TAG, "failed to configure sensor GPIO %d", config->sensor_gpio);
        return false;
    }

    gpio_config_t siren_cfg = {
        .pin_bit_mask = 1ULL << config->siren_gpio,
        .mode = GPIO_MODE_OUTPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };
    if (gpio_config(&siren_cfg) != ESP_OK) {
        ESP_LOGE(TAG, "failed to configure siren GPIO %d", config->siren_gpio);
        return false;
    }
    write_siren(config, false); /* safe default: siren off until first poll decides otherwise */

    s_state.last_sensor_active = read_sensor_active(config);

    const esp_timer_create_args_t timer_args = {
        .callback = &poll_timer_cb,
        .arg = NULL,
        .name = "sensor_siren_poll",
    };
    if (esp_timer_create(&timer_args, &s_timer) != ESP_OK) {
        ESP_LOGE(TAG, "failed to create poll timer");
        return false;
    }
    if (esp_timer_start_periodic(s_timer, (uint64_t)config->poll_interval_ms * 1000ULL) != ESP_OK) {
        ESP_LOGE(TAG, "failed to start poll timer");
        return false;
    }
    return true;
}

void sensor_siren_port_stop(void)
{
    if (s_timer != NULL) {
        esp_timer_stop(s_timer);
        esp_timer_delete(s_timer);
        s_timer = NULL;
    }
}
