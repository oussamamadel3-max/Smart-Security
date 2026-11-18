#include "event_queue.h"
#include <string.h>

bool event_queue_init(event_queue_t *queue, uint16_t capacity)
{
    if (queue == NULL || capacity == 0 || capacity > EVENT_QUEUE_MAX_CAPACITY) {
        return false;
    }
    memset(queue, 0, sizeof(*queue));
    queue->capacity = capacity;
    return true;
}

bool event_queue_enqueue(event_queue_t *queue, uint32_t event_uid, bool alarm_phase_ended,
                         esp_core_time_ms_t occurred_at_ms, bool was_buffered)
{
    if (queue->count >= queue->capacity) {
        queue->loss_flag = true;
        return false;
    }
    for (uint16_t i = 0; i < queue->capacity; i++) {
        if (!queue->slots[i].in_use) {
            queue->slots[i].event_uid = event_uid;
            queue->slots[i].alarm_phase_ended = alarm_phase_ended;
            queue->slots[i].occurred_at_ms = occurred_at_ms;
            queue->slots[i].was_buffered = was_buffered;
            queue->slots[i].in_use = true;
            queue->slots[i].enqueue_seq = queue->next_enqueue_seq++;
            queue->count++;
            return true;
        }
    }
    /* Unreachable if count/capacity bookkeeping is correct. */
    queue->loss_flag = true;
    return false;
}

bool event_queue_ack(event_queue_t *queue, uint32_t event_uid, bool alarm_phase_ended)
{
    for (uint16_t i = 0; i < queue->capacity; i++) {
        if (queue->slots[i].in_use &&
            queue->slots[i].event_uid == event_uid &&
            queue->slots[i].alarm_phase_ended == alarm_phase_ended) {
            queue->slots[i].in_use = false;
            queue->count--;
            return true;
        }
    }
    return false;
}

bool event_queue_peek_oldest(const event_queue_t *queue, event_record_t *out)
{
    /* Slot index order does not reflect insertion order once a freed slot is
     * reused by a later enqueue, so pick the minimum enqueue_seq among in-use
     * slots rather than the lowest index. */
    bool found = false;
    event_record_t oldest;
    for (uint16_t i = 0; i < queue->capacity; i++) {
        if (queue->slots[i].in_use && (!found || queue->slots[i].enqueue_seq < oldest.enqueue_seq)) {
            oldest = queue->slots[i];
            found = true;
        }
    }
    if (found) {
        *out = oldest;
    }
    return found;
}

uint16_t event_queue_count(const event_queue_t *queue)
{
    return queue->count;
}

bool event_queue_is_full(const event_queue_t *queue)
{
    return queue->count >= queue->capacity;
}

bool event_queue_consume_loss_flag(event_queue_t *queue)
{
    bool flag = queue->loss_flag;
    queue->loss_flag = false;
    return flag;
}
