/*
 * Classify inbound commands-topic payloads by messageType (draft v1).
 * Does not invent message types beyond the draft README table.
 */
#ifndef INBOUND_MESSAGE_H
#define INBOUND_MESSAGE_H

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    INBOUND_MSG_UNKNOWN = 0,
    INBOUND_MSG_EXECUTE_COMMAND,
    INBOUND_MSG_OWNERSHIP_SYNC,
    INBOUND_MSG_EVENT_PERSISTED
    /* SETTINGS_SYNC intentionally absent — deferred outside draft v1. */
} inbound_message_type_t;

/* Reads the JSON "messageType" string only (lightweight). Returns UNKNOWN
 * when missing/unsupported. Does not validate the rest of the payload. */
inbound_message_type_t inbound_message_classify(const char *json_text);

#ifdef __cplusplus
}
#endif

#endif /* INBOUND_MESSAGE_H */
