/*
 * Persist / restore the in-RAM event_queue_t via storage_backend_t so buffered
 * ALARM_EVENT records survive reboot until EVENT_PERSISTED (T-05 continuity).
 * Host-testable; real NVS adapter is unchanged.
 */
#ifndef EVENT_QUEUE_PERSIST_H
#define EVENT_QUEUE_PERSIST_H

#include "event_queue.h"
#include "storage_backend.h"

#ifdef __cplusplus
extern "C" {
#endif

#define EVENT_QUEUE_PERSIST_KEY "event_queue_v1"

typedef enum {
    EVENT_QUEUE_PERSIST_OK = 0,
    EVENT_QUEUE_PERSIST_NOT_FOUND,
    EVENT_QUEUE_PERSIST_CORRUPTED
} event_queue_persist_result_t;

bool event_queue_persist_save(const storage_backend_t *backend, const event_queue_t *queue);

event_queue_persist_result_t event_queue_persist_load(const storage_backend_t *backend, event_queue_t *queue);

#ifdef __cplusplus
}
#endif

#endif /* EVENT_QUEUE_PERSIST_H */
