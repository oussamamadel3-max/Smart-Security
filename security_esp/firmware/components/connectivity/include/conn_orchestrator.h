/*
 * Wires core + wire_json + Wi-Fi/MQTT/SNTP/provisioning into one FreeRTOS
 * lifecycle. Local protection (sensor_siren_port) must already be running
 * independently — this module never gates the siren path on network state.
 *
 * Experimental draft MQTT only (G1 not adopted). Broker URI / optional lab
 * credentials come from NVS keys under namespace "mqtt_cfg"; absent keys
 * mean networking stays idle after Wi-Fi (or provisioning) without inventing
 * production endpoints.
 */
#ifndef CONN_ORCHESTRATOR_H
#define CONN_ORCHESTRATOR_H

#include "protection_core.h"
#include "ownership_state.h"
#include "event_queue.h"
#include "sequence_counter.h"
#include "command_dedup.h"
#include "storage_backend.h"
#include "device_identity.h"
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    protection_core_t *core;
    ownership_state_t *ownership;
    event_queue_t *events;
    sequence_counter_t *sequences;
    command_dedup_t *dedup;
    storage_backend_t *storage; /* for event_queue_persist after ack/enqueue */
    device_identity_t identity;
} conn_orchestrator_refs_t;

/* Starts Wi-Fi (or SoftAP provisioning), SNTP, MQTT, and a FreeRTOS worker.
 * Returns false if identity/wifi init fails hard. Safe to call once. */
bool conn_orchestrator_start(conn_orchestrator_refs_t *refs);

/* Ask the worker to publish a STATE snapshot soon (e.g. after local arm change). */
void conn_orchestrator_request_state_publish(void);

/* Enqueue + optionally publish an alarm event (was_buffered depends on MQTT). */
bool conn_orchestrator_on_alarm_event(uuid128_t event_uid, bool phase_ended);

bool conn_orchestrator_is_mqtt_ready(void);

bool conn_orchestrator_clock_trusted(void);

#ifdef __cplusplus
}
#endif

#endif /* CONN_ORCHESTRATOR_H */
