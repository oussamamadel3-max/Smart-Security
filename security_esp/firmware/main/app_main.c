/*
 * Application entry: wires core ↔ port_esp32 ↔ connectivity.
 *
 * Ordering guarantees:
 *   1) NVS init
 *   2) Restore arm_state + stateSequence + event queue from NVS
 *   3) Start local sensor/siren path (independent of network)
 *   4) Start connectivity orchestrator (Wi-Fi / SoftAP / SNTP / MQTT)
 *
 * GPIO pins below are PLACEHOLDERS pending D-01. Detector/test durations are
 * D-03 reference values only. Never flashed in this project.
 */
#include "esp_log.h"
#include "nvs_flash.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

#include "protection_core.h"
#include "persisted_state.h"
#include "sequence_counter.h"
#include "event_queue.h"
#include "event_queue_persist.h"
#include "ownership_state.h"
#include "command_dedup.h"
#include "nvs_storage_backend.h"
#include "hw_rng.h"
#include "sensor_siren_port.h"
#include "conn_orchestrator.h"

static const char *TAG = "smart_security_main";

#define PLACEHOLDER_SENSOR_GPIO 4
#define PLACEHOLDER_SIREN_GPIO 5
#define SEQUENCE_BATCH_SIZE 64u /* wear-vs-skip tradeoff; not D-03 production */
#define EVENT_QUEUE_CAPACITY 16u /* reference only; NFR-008 open */

static protection_core_t s_core;
static ownership_state_t s_ownership;
static event_queue_t s_events;
static sequence_counter_t s_sequences;
static command_dedup_t s_dedup;
static nvs_storage_ctx_t s_storage_ctx = { .nvs_namespace = "sec_state" };
static storage_backend_t s_storage;
static conn_orchestrator_refs_t s_conn_refs;

void app_main(void)
{
    esp_err_t nvs_err = nvs_flash_init();
    if (nvs_err == ESP_ERR_NVS_NO_FREE_PAGES || nvs_err == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        ESP_LOGW(TAG, "NVS partition needs erase (err=%s) — erasing and retrying", esp_err_to_name(nvs_err));
        ESP_ERROR_CHECK(nvs_flash_erase());
        nvs_err = nvs_flash_init();
    }
    ESP_ERROR_CHECK(nvs_err);

    s_storage = nvs_storage_backend_create(&s_storage_ctx);

    alarm_detector_config_t detector_config = {
        .pulses_to_qualify = 3,
        .window_ms = 2000,
        .cooldown_ms = 5000,
    };
    bool ok = protection_core_init(&s_core, &detector_config, 30000, hw_rng_fill, NULL);
    ESP_LOGI(TAG, "protection_core_init: %s", ok ? "OK" : "FAILED");

    ownership_state_init(&s_ownership);
    command_dedup_init(&s_dedup);

    if (!sequence_counter_init(&s_sequences, &s_storage, SEQUENCE_BATCH_SIZE)) {
        ESP_LOGE(TAG, "sequence_counter_init FAILED");
    } else {
        ESP_LOGI(TAG, "stateSequence restored/reserved from NVS");
    }

    event_queue_persist_result_t eqr = event_queue_persist_load(&s_storage, &s_events);
    if (eqr == EVENT_QUEUE_PERSIST_OK) {
        ESP_LOGI(TAG, "event queue restored from NVS (count=%u)", (unsigned)event_queue_count(&s_events));
    } else {
        if (eqr == EVENT_QUEUE_PERSIST_CORRUPTED) {
            ESP_LOGE(TAG, "event queue CORRUPTED — starting empty (loss may have occurred)");
        }
        if (!event_queue_init(&s_events, EVENT_QUEUE_CAPACITY)) {
            ESP_LOGE(TAG, "event_queue_init FAILED");
        }
    }

    persisted_state_record_t saved;
    persisted_state_load_result_t load_result = persisted_state_load(&s_storage, &saved);
    if (load_result == PERSISTED_STATE_LOAD_OK) {
        ESP_LOGI(TAG, "restored confirmed arm_state=%d from NVS", saved.arm_state);
        if (saved.arm_state == ARM_STATE_ARMED) {
            protection_core_arm(&s_core);
        }
    } else if (load_result == PERSISTED_STATE_LOAD_CORRUPTED) {
        ESP_LOGE(TAG, "persisted_state CORRUPTED — staying DISARMED, no production safe-behaviour policy approved yet");
    } else {
        ESP_LOGI(TAG, "no persisted arm_state (first boot)");
    }

    /* Local alarm path — MUST start regardless of Wi-Fi/MQTT. */
    sensor_siren_port_config_t port_config = {
        .sensor_gpio = PLACEHOLDER_SENSOR_GPIO,
        .siren_gpio = PLACEHOLDER_SIREN_GPIO,
        .invert_sensor_level = false,
        .invert_siren_level = false,
        .poll_interval_ms = 10,
    };
    if (!sensor_siren_port_start(&port_config, &s_core)) {
        ESP_LOGE(TAG, "sensor_siren_port_start FAILED — local alarm path is NOT active");
    } else {
        ESP_LOGI(TAG, "sensor_siren_port started (local path independent of network)");
    }

    s_conn_refs.core = &s_core;
    s_conn_refs.ownership = &s_ownership;
    s_conn_refs.events = &s_events;
    s_conn_refs.sequences = &s_sequences;
    s_conn_refs.dedup = &s_dedup;
    s_conn_refs.storage = &s_storage;

    if (!conn_orchestrator_start(&s_conn_refs)) {
        ESP_LOGW(TAG, "conn_orchestrator_start failed — local protection still running");
    } else {
        ESP_LOGI(TAG, "connectivity started (experimental draft MQTT; G1 not adopted)");
    }
}
