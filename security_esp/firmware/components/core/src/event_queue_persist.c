#include "event_queue_persist.h"
#include <string.h>
#include <stddef.h>

typedef struct {
    uint32_t version;
    uint16_t capacity;
    uint16_t count;
    uint32_t next_enqueue_seq;
    uint8_t loss_flag;
    uint8_t reserved[3];
    event_record_t slots[EVENT_QUEUE_MAX_CAPACITY];
    uint32_t checksum;
} event_queue_blob_t;

#define EVENT_QUEUE_BLOB_VERSION 1u
/* Use offsetof — NOT (sizeof - sizeof(checksum)) — because trailing struct
 * padding after checksum would otherwise include the checksum bytes in the
 * hashed prefix and break round-trips once checksum is non-zero. */
#define EVENT_QUEUE_BLOB_PAYLOAD_SIZE (offsetof(event_queue_blob_t, checksum))

bool event_queue_persist_save(const storage_backend_t *backend, const event_queue_t *queue)
{
    if (backend == NULL || queue == NULL || backend->save == NULL) {
        return false;
    }
    event_queue_blob_t blob;
    memset(&blob, 0, sizeof(blob));
    blob.version = EVENT_QUEUE_BLOB_VERSION;
    blob.capacity = queue->capacity;
    blob.count = queue->count;
    blob.next_enqueue_seq = queue->next_enqueue_seq;
    blob.loss_flag = queue->loss_flag ? 1u : 0u;
    memcpy(blob.slots, queue->slots, sizeof(blob.slots));
    blob.checksum = storage_checksum(&blob, EVENT_QUEUE_BLOB_PAYLOAD_SIZE);
    return backend->save(backend->ctx, EVENT_QUEUE_PERSIST_KEY, &blob, sizeof(blob));
}

event_queue_persist_result_t event_queue_persist_load(const storage_backend_t *backend, event_queue_t *queue)
{
    if (backend == NULL || queue == NULL || backend->load == NULL) {
        return EVENT_QUEUE_PERSIST_CORRUPTED;
    }
    event_queue_blob_t blob;
    memset(&blob, 0, sizeof(blob));
    if (!backend->load(backend->ctx, EVENT_QUEUE_PERSIST_KEY, &blob, sizeof(blob))) {
        return EVENT_QUEUE_PERSIST_NOT_FOUND;
    }
    uint32_t expected = storage_checksum(&blob, EVENT_QUEUE_BLOB_PAYLOAD_SIZE);
    if (blob.checksum != expected || blob.version != EVENT_QUEUE_BLOB_VERSION) {
        return EVENT_QUEUE_PERSIST_CORRUPTED;
    }
    if (blob.capacity == 0 || blob.capacity > EVENT_QUEUE_MAX_CAPACITY || blob.count > blob.capacity) {
        return EVENT_QUEUE_PERSIST_CORRUPTED;
    }
    if (!event_queue_init(queue, blob.capacity)) {
        return EVENT_QUEUE_PERSIST_CORRUPTED;
    }
    queue->count = blob.count;
    queue->next_enqueue_seq = blob.next_enqueue_seq;
    queue->loss_flag = blob.loss_flag != 0;
    memcpy(queue->slots, blob.slots, sizeof(queue->slots));
    return EVENT_QUEUE_PERSIST_OK;
}
