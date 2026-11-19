#include "conn_orchestrator.h"
#include "wifi_manager.h"
#include "wifi_provisioning_ap.h"
#include "mqtt_transport.h"
#include "sntp_sync.h"
#include "connection_lifecycle.h"
#include "mqtt_topics.h"
#include "inbound_message.h"
#include "execute_command_wire.h"
#include "ownership_sync_wire.h"
#include "event_persisted_wire.h"
#include "command_ack_json.h"
#include "state_json.h"
#include "heartbeat_json.h"
#include "alarm_event_json.h"
#include "event_loss_json.h"
#include "remote_command.h"
#include "command_ack_builder.h"
#include "persisted_state.h"
#include "event_queue_persist.h"
#include "hw_rng.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/semphr.h"
#include "nvs.h"
#include <string.h>
#include <stdlib.h>

static const char *TAG = "conn_orch";

static conn_orchestrator_refs_t *s_refs;
static conn_lifecycle_t s_life;
static SemaphoreHandle_t s_lock;
static volatile bool s_state_requested;
static volatile bool s_started;
static char s_broker_uri[128];
static char s_mqtt_user[64];
static char s_mqtt_pass[64];
static uint64_t s_last_applied_command_sequence;

static void load_mqtt_cfg_from_nvs(void)
{
    s_broker_uri[0] = s_mqtt_user[0] = s_mqtt_pass[0] = '\0';
    nvs_handle_t h;
    if (nvs_open("mqtt_cfg", NVS_READONLY, &h) != ESP_OK) {
        ESP_LOGW(TAG, "no mqtt_cfg NVS — networking will idle after Wi-Fi (no invented broker)");
        return;
    }
    size_t len = sizeof(s_broker_uri);
    nvs_get_str(h, "broker_uri", s_broker_uri, &len);
    len = sizeof(s_mqtt_user);
    nvs_get_str(h, "username", s_mqtt_user, &len);
    len = sizeof(s_mqtt_pass);
    nvs_get_str(h, "password", s_mqtt_pass, &len);
    nvs_close(h);
}

static bool make_message_id(char out[37])
{
    uuid128_t id = uuid128_generate_v4(hw_rng_fill, NULL);
    uuid128_to_string(&id, out);
    return true;
}

static void persist_events(void)
{
    if (s_refs && s_refs->storage && s_refs->events) {
        (void)event_queue_persist_save(s_refs->storage, s_refs->events);
    }
}

static void publish_state_locked(void)
{
    if (!mqtt_transport_is_connected() || s_refs == NULL) {
        return;
    }
    char msg_id[37];
    make_message_id(msg_id);
    state_snapshot_t snap = {
        .state_sequence = sequence_counter_next(s_refs->sequences),
        .confirmed_ownership_version = s_refs->ownership->version,
        .arm_state = s_refs->core->arm_state,
        .siren_state = protection_core_siren_state(s_refs->core),
        .health_state = HEALTH_STATE_UNKNOWN, /* no measurement circuit yet (T-08) */
        .has_current_alarm_event_uid = s_refs->core->current_event.is_active,
        .current_alarm_event_uid = s_refs->core->current_event.uid,
        .clock_trusted = sntp_sync_is_clock_trusted(),
    };
    char *json = state_to_json(&snap, s_refs->identity.device_id, msg_id, s_refs->identity.boot_id);
    if (json == NULL) {
        return;
    }
    char topic[MQTT_TOPIC_MAX_LEN];
    if (mqtt_topic_state(s_refs->identity.device_id, topic, sizeof(topic))) {
        /* STATE QoS not fixed for all cases in draft; use 1 for authoritative snapshots. */
        mqtt_transport_publish(topic, json, 1, false);
    }
    free(json);
}

static void publish_heartbeat(void)
{
    if (!mqtt_transport_is_connected() || s_refs == NULL) {
        return;
    }
    char msg_id[37];
    make_message_id(msg_id);
    char *json = heartbeat_to_json(s_refs->identity.device_id, msg_id, s_refs->identity.boot_id,
                                   sntp_sync_is_clock_trusted());
    if (json == NULL) {
        return;
    }
    char topic[MQTT_TOPIC_MAX_LEN];
    if (mqtt_topic_state(s_refs->identity.device_id, topic, sizeof(topic))) {
        mqtt_transport_publish(topic, json, 0, false); /* HEARTBEAT QoS 0 per draft */
    }
    free(json);
}

static void publish_ack(const command_ack_t *ack, const char *command_id)
{
    char msg_id[37];
    make_message_id(msg_id);
    char *json = command_ack_to_json(ack, s_refs->identity.device_id, msg_id,
                                     s_refs->identity.boot_id, command_id);
    if (json == NULL) {
        return;
    }
    char topic[MQTT_TOPIC_MAX_LEN];
    if (mqtt_topic_acks(s_refs->identity.device_id, topic, sizeof(topic))) {
        mqtt_transport_publish(topic, json, 1, false);
    }
    free(json);
}

static void handle_execute_command(const char *payload)
{
    execute_command_wire_t wire;
    char err[96];
    if (!execute_command_parse(payload, &wire, err, sizeof(err))) {
        ESP_LOGW(TAG, "EXECUTE_COMMAND parse failed: %s", err);
        return;
    }
    if (strcmp(wire.device_id, s_refs->identity.device_id) != 0) {
        ESP_LOGW(TAG, "EXECUTE_COMMAND deviceId mismatch — ignored");
        return;
    }

    execute_command_t cmd;
    execute_command_wire_to_core(&wire, &cmd);

    remote_command_context_t ctx = {
        .last_applied_command_sequence = s_last_applied_command_sequence,
        .current_ownership_version = s_refs->ownership->version,
        .clock_trusted = sntp_sync_is_clock_trusted(),
        .now_ms = (esp_core_time_ms_t)(sntp_sync_is_clock_trusted()
                                           ? (uint64_t)sntp_sync_now_epoch_ms()
                                           : 0),
    };
    remote_command_verify_result_t vr = remote_command_verify(&cmd, &ctx, s_refs->dedup);

    bool executed = false;
    const char *reject = NULL;
    switch (vr) {
        case REMOTE_COMMAND_VERIFY_ACCEPT:
            switch (cmd.type) {
                case REMOTE_COMMAND_TYPE_ARM:
                    protection_core_arm(s_refs->core);
                    if (s_refs->storage) {
                        persisted_state_save(s_refs->storage, ARM_STATE_ARMED);
                    }
                    executed = true;
                    break;
                case REMOTE_COMMAND_TYPE_DISARM:
                    protection_core_disarm(s_refs->core);
                    if (s_refs->storage) {
                        persisted_state_save(s_refs->storage, ARM_STATE_DISARMED);
                    }
                    executed = true;
                    break;
                case REMOTE_COMMAND_TYPE_MUTE_ALARM:
                    if (cmd.has_target_event) {
                        protection_cmd_result_t mr = protection_core_mute(s_refs->core, cmd.target_event_uid);
                        executed = (mr == PROTECTION_CMD_OK);
                        if (!executed) {
                            reject = "MUTE_REJECTED";
                        }
                    } else {
                        reject = "MUTE_MISSING_TARGET";
                    }
                    break;
                case REMOTE_COMMAND_TYPE_TEST_SIREN: {
                    protection_cmd_result_t tr = protection_core_start_test(s_refs->core, (esp_core_time_ms_t)(xTaskGetTickCount() * portTICK_PERIOD_MS));
                    executed = (tr == PROTECTION_CMD_OK);
                    if (!executed) {
                        reject = "TEST_REJECTED";
                    }
                    break;
                }
                case REMOTE_COMMAND_TYPE_STOP_TEST: {
                    protection_cmd_result_t sr = protection_core_stop_test(s_refs->core);
                    executed = (sr == PROTECTION_CMD_OK);
                    if (!executed) {
                        reject = "STOP_TEST_REJECTED";
                    }
                    break;
                }
            }
            if (executed) {
                s_last_applied_command_sequence = cmd.command_sequence;
            }
            break;
        case REMOTE_COMMAND_VERIFY_DUPLICATE:
            /* Resend ACK without re-executing — build from current state as EXECUTED. */
            executed = true;
            break;
        case REMOTE_COMMAND_VERIFY_REJECT_OWNERSHIP_MISMATCH:
            reject = "OWNERSHIP_MISMATCH";
            break;
        case REMOTE_COMMAND_VERIFY_REJECT_STALE_SEQUENCE:
            reject = "STALE_SEQUENCE";
            break;
        case REMOTE_COMMAND_VERIFY_REJECT_CLOCK_NOT_TRUSTED:
            reject = "CLOCK_NOT_TRUSTED";
            break;
        case REMOTE_COMMAND_VERIFY_REJECT_EXPIRED:
            reject = "EXPIRED";
            break;
    }

    uint64_t seq = sequence_counter_next(s_refs->sequences);
    command_ack_t ack = command_ack_build(s_refs->core, HEALTH_STATE_UNKNOWN, s_refs->ownership,
                                          sntp_sync_is_clock_trusted(), seq, executed, reject);
    publish_ack(&ack, wire.command_id);
    publish_state_locked();
}

static void handle_ownership_sync(const char *payload)
{
    ownership_sync_wire_t wire;
    char err[96];
    if (!ownership_sync_parse(payload, &wire, err, sizeof(err))) {
        ESP_LOGW(TAG, "OWNERSHIP_SYNC parse failed: %s", err);
        return;
    }
    if (strcmp(wire.device_id, s_refs->identity.device_id) != 0) {
        return;
    }
    ownership_state_apply_sync(s_refs->ownership, wire.ownership_version, wire.is_linked);
    ESP_LOGI(TAG, "OWNERSHIP_SYNC applied version=%llu linked=%d",
             (unsigned long long)wire.ownership_version, (int)wire.is_linked);
    publish_state_locked(); /* confirm via confirmedOwnershipVersion on STATE */
}

static void handle_event_persisted(const char *payload)
{
    event_persisted_wire_t wire;
    char err[96];
    if (!event_persisted_parse(payload, &wire, err, sizeof(err))) {
        ESP_LOGW(TAG, "EVENT_PERSISTED parse failed: %s", err);
        return;
    }
    if (strcmp(wire.device_id, s_refs->identity.device_id) != 0) {
        return;
    }
    bool ended = wire.has_alarm_phase ? wire.alarm_phase_ended : false;
    if (event_queue_ack(s_refs->events, wire.event_uid, ended)) {
        persist_events();
        ESP_LOGI(TAG, "EVENT_PERSISTED acked from queue");
    }
}

static void on_mqtt_message(const char *topic, const char *payload, int payload_len, void *ctx)
{
    (void)topic;
    (void)payload_len;
    (void)ctx;
    if (s_refs == NULL || payload == NULL) {
        return;
    }
    if (xSemaphoreTake(s_lock, pdMS_TO_TICKS(2000)) != pdTRUE) {
        return;
    }
    inbound_message_type_t kind = inbound_message_classify(payload);
    switch (kind) {
        case INBOUND_MSG_EXECUTE_COMMAND:
            handle_execute_command(payload);
            break;
        case INBOUND_MSG_OWNERSHIP_SYNC:
            handle_ownership_sync(payload);
            break;
        case INBOUND_MSG_EVENT_PERSISTED:
            handle_event_persisted(payload);
            break;
        default:
            ESP_LOGW(TAG, "unknown/unsupported inbound messageType");
            break;
    }
    xSemaphoreGive(s_lock);
}

static void on_mqtt_connected(void *ctx)
{
    (void)ctx;
    conn_lifecycle_on_mqtt_connected(&s_life);
    char topic[MQTT_TOPIC_MAX_LEN];
    if (mqtt_topic_commands(s_refs->identity.device_id, topic, sizeof(topic))) {
        mqtt_transport_subscribe(topic, 1);
    }
    if (xSemaphoreTake(s_lock, pdMS_TO_TICKS(2000)) == pdTRUE) {
        publish_state_locked();
        /* Drain loss flag once per reconnect episode. */
        if (event_queue_consume_loss_flag(s_refs->events)) {
            char msg_id[37];
            make_message_id(msg_id);
            uuid128_t report = uuid128_generate_v4(hw_rng_fill, NULL);
            char *json = event_loss_to_json(s_refs->identity.device_id, msg_id, s_refs->identity.boot_id,
                                            &report, s_refs->ownership->version, sntp_sync_is_clock_trusted());
            if (json) {
                char etopic[MQTT_TOPIC_MAX_LEN];
                if (mqtt_topic_events(s_refs->identity.device_id, etopic, sizeof(etopic))) {
                    mqtt_transport_publish(etopic, json, 1, false);
                }
                free(json);
            }
        }
        xSemaphoreGive(s_lock);
    }
}

static void on_mqtt_disconnected(void *ctx)
{
    (void)ctx;
    conn_lifecycle_on_transport_lost(&s_life);
}

static void start_mqtt_if_configured(void)
{
    if (s_broker_uri[0] == '\0') {
        ESP_LOGW(TAG, "broker_uri empty — MQTT not started (set NVS mqtt_cfg/broker_uri for lab)");
        return;
    }
    conn_lifecycle_begin_mqtt(&s_life);
    mqtt_transport_config_t cfg = {
        .broker_uri = s_broker_uri,
        .client_id = s_refs->identity.device_id,
        .username = s_mqtt_user[0] ? s_mqtt_user : NULL,
        .password = s_mqtt_pass[0] ? s_mqtt_pass : NULL,
        .ca_cert_pem = NULL, /* mqtts requires explicit CA via future NVS key — not invented */
        .on_message = on_mqtt_message,
        .on_connected = on_mqtt_connected,
        .on_disconnected = on_mqtt_disconnected,
        .ctx = NULL,
    };
    if (!mqtt_transport_start(&cfg)) {
        ESP_LOGE(TAG, "mqtt_transport_start failed");
        conn_lifecycle_on_transport_lost(&s_life);
    }
}

static void on_wifi_connected(void *ctx)
{
    (void)ctx;
    conn_lifecycle_on_wifi_connected(&s_life);
    conn_lifecycle_begin_time_sync(&s_life);
    sntp_sync_start(NULL);
    conn_lifecycle_on_time_sync_done(&s_life);
    start_mqtt_if_configured();
}

static void on_wifi_disconnected(void *ctx)
{
    (void)ctx;
    conn_lifecycle_on_transport_lost(&s_life);
}

static void on_prov_done(bool saved_ok, void *ctx)
{
    (void)ctx;
    if (saved_ok) {
        ESP_LOGI(TAG, "credentials saved — starting STA");
        conn_lifecycle_begin_wifi(&s_life);
        wifi_manager_start_sta();
    }
}

static void drain_event_queue(void)
{
    if (!mqtt_transport_is_connected()) {
        return;
    }
    event_record_t rec;
    if (!event_queue_peek_oldest(s_refs->events, &rec)) {
        return;
    }
    char msg_id[37];
    make_message_id(msg_id);
    alarm_event_wire_t ev = {
        .event_uid = rec.event_uid,
        .ownership_version = s_refs->ownership->version,
        .sensor_key = "vibration-1",
        .was_buffered = rec.was_buffered,
        .clock_trusted = sntp_sync_is_clock_trusted(),
        .alarm_phase_ended = rec.alarm_phase_ended,
        .include_alarm_phase = true,
    };
    char *json = alarm_event_to_json(&ev, s_refs->identity.device_id, msg_id, s_refs->identity.boot_id);
    if (json == NULL) {
        return;
    }
    char topic[MQTT_TOPIC_MAX_LEN];
    if (mqtt_topic_events(s_refs->identity.device_id, topic, sizeof(topic))) {
        mqtt_transport_publish(topic, json, 1, false);
    }
    free(json);
}

static void worker_task(void *arg)
{
    (void)arg;
    uint32_t hb_ticks = 0;
    for (;;) {
        if (s_state_requested) {
            s_state_requested = false;
            if (xSemaphoreTake(s_lock, pdMS_TO_TICKS(1000)) == pdTRUE) {
                publish_state_locked();
                xSemaphoreGive(s_lock);
            }
        }
        if (xSemaphoreTake(s_lock, pdMS_TO_TICKS(1000)) == pdTRUE) {
            drain_event_queue();
            xSemaphoreGive(s_lock);
        }
        hb_ticks++;
        if (hb_ticks >= 60) { /* ~60s heartbeat cadence — lab default, not D-03 production */
            hb_ticks = 0;
            publish_heartbeat();
        }
        vTaskDelay(pdMS_TO_TICKS(1000));
    }
}

bool conn_orchestrator_start(conn_orchestrator_refs_t *refs)
{
    if (s_started || refs == NULL || refs->core == NULL) {
        return false;
    }
    s_refs = refs;
    s_lock = xSemaphoreCreateMutex();
    if (s_lock == NULL) {
        return false;
    }
    conn_lifecycle_init(&s_life, 1000, 60000);
    load_mqtt_cfg_from_nvs();

    if (!device_identity_init(&refs->identity, hw_rng_fill, NULL)) {
        ESP_LOGE(TAG, "device_identity_init failed");
        return false;
    }

    wifi_manager_callbacks_t wcbs = {
        .on_connected = on_wifi_connected,
        .on_disconnected = on_wifi_disconnected,
        .ctx = NULL,
    };
    if (!wifi_manager_init(&wcbs)) {
        return false;
    }

    if (wifi_manager_has_credentials()) {
        conn_lifecycle_begin_wifi(&s_life);
        wifi_manager_start_sta();
    } else {
        wifi_provisioning_ap_config_t pcfg = {
            .ap_ssid = "SmartSecurity-Setup",
            .timeout_ms = 10u * 60u * 1000u, /* 10 minutes lab timeout */
            .on_done = on_prov_done,
            .ctx = NULL,
        };
        wifi_provisioning_ap_start(&pcfg);
    }

    if (xTaskCreate(worker_task, "conn_worker", 6144, NULL, 5, NULL) != pdPASS) {
        ESP_LOGE(TAG, "worker task create failed");
        return false;
    }
    s_started = true;
    ESP_LOGI(TAG, "connectivity orchestrator started (deviceId=%s)", refs->identity.device_id);
    return true;
}

void conn_orchestrator_request_state_publish(void)
{
    s_state_requested = true;
}

bool conn_orchestrator_on_alarm_event(uuid128_t event_uid, bool phase_ended)
{
    if (s_refs == NULL || s_refs->events == NULL) {
        return false;
    }
    bool buffered = !mqtt_transport_is_connected();
    bool stored = event_queue_enqueue(s_refs->events, event_uid, phase_ended,
                                      (esp_core_time_ms_t)(xTaskGetTickCount() * portTICK_PERIOD_MS),
                                      buffered);
    persist_events();
    return stored;
}

bool conn_orchestrator_is_mqtt_ready(void)
{
    return conn_lifecycle_is_mqtt_ready(&s_life) && mqtt_transport_is_connected();
}

bool conn_orchestrator_clock_trusted(void)
{
    return sntp_sync_is_clock_trusted();
}
