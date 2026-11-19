/*
 * T-05 — bounded local queue of alarm-event records pending EVENT_PERSISTED
 * acknowledgement from the backend.
 *
 * Non-negotiable invariant (T-05 acceptance, AC-T-09): a full queue must
 * never stop the local siren/alarm path, and must never evict a record that
 * hasn't been acknowledged yet. So when the queue is full, a *new* record is
 * simply not stored, and a separate, independent loss flag is raised instead
 * (the "حيز مستقل أو محجوز لعلامة الفقد" requirement) — nothing already
 * queued is sacrificed to make room.
 *
 * was_buffered is carried through, not computed, here: whether an event "was
 * buffered" (held locally rather than sent at origination time) depends on
 * the network layer's connectivity state at enqueue time, which this module
 * has no visibility into (T-06 scope). The caller supplies it.
 */
#ifndef EVENT_QUEUE_H
#define EVENT_QUEUE_H

#include "esp_core_types.h"

#ifdef __cplusplus
extern "C" {
#endif

#define EVENT_QUEUE_MAX_CAPACITY 32

typedef struct {
    uuid128_t event_uid;     /* matches alarm_event_id_t.uid / the backend's AlarmEvent.EventUid (Guid) */
    bool alarm_phase_ended;  /* false = ACTIVE, true = ENDED (docs/contracts/state-machines.md alarmPhase) */
    esp_core_time_ms_t occurred_at_ms;
    bool was_buffered;
    bool in_use;
    uint32_t enqueue_seq;    /* insertion order, independent of slot index (slots are reused once acked) */
} event_record_t;

typedef struct {
    event_record_t slots[EVENT_QUEUE_MAX_CAPACITY];
    uint16_t capacity;   /* <= EVENT_QUEUE_MAX_CAPACITY; the real production number is D-03/NFR-008, still OPEN */
    uint16_t count;
    uint32_t next_enqueue_seq;
    bool loss_flag;
} event_queue_t;

bool event_queue_init(event_queue_t *queue, uint16_t capacity);

/* Returns true if stored. Returns false and sets the loss flag (retrievable
 * via event_queue_consume_loss_flag) if the queue is already at capacity —
 * the record is dropped, nothing existing is evicted. */
bool event_queue_enqueue(event_queue_t *queue, uuid128_t event_uid, bool alarm_phase_ended,
                         esp_core_time_ms_t occurred_at_ms, bool was_buffered);

/* Removes the record matching (event_uid, alarm_phase_ended) once the backend
 * has confirmed EVENT_PERSISTED for that exact phase. Returns false if no
 * matching record is queued (e.g. already acked, or never buffered because it
 * was sent and acked in real time without ever needing this queue). */
bool event_queue_ack(event_queue_t *queue, uuid128_t event_uid, bool alarm_phase_ended);

/* Oldest not-yet-acked record, for the network layer to (re)attempt sending.
 * Returns false if the queue is empty. */
bool event_queue_peek_oldest(const event_queue_t *queue, event_record_t *out);

uint16_t event_queue_count(const event_queue_t *queue);

bool event_queue_is_full(const event_queue_t *queue);

/* Returns the current loss flag and clears it — call once when connectivity
 * resumes so the device can report "events were lost" exactly once per loss
 * episode, not once per dropped event. */
bool event_queue_consume_loss_flag(event_queue_t *queue);

#ifdef __cplusplus
}
#endif

#endif /* EVENT_QUEUE_H */
