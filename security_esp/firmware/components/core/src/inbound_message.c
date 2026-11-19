#include "inbound_message.h"
#include <string.h>
#include <ctype.h>

static const char *find_message_type_value(const char *json)
{
    if (json == NULL) {
        return NULL;
    }
    const char *key = strstr(json, "\"messageType\"");
    if (key == NULL) {
        return NULL;
    }
    const char *colon = strchr(key + 13, ':');
    if (colon == NULL) {
        return NULL;
    }
    const char *p = colon + 1;
    while (*p != '\0' && isspace((unsigned char)*p)) {
        p++;
    }
    if (*p != '"') {
        return NULL;
    }
    return p + 1;
}

inbound_message_type_t inbound_message_classify(const char *json_text)
{
    const char *value = find_message_type_value(json_text);
    if (value == NULL) {
        return INBOUND_MSG_UNKNOWN;
    }
    if (strncmp(value, "EXECUTE_COMMAND\"", 16) == 0) {
        return INBOUND_MSG_EXECUTE_COMMAND;
    }
    if (strncmp(value, "OWNERSHIP_SYNC\"", 15) == 0) {
        return INBOUND_MSG_OWNERSHIP_SYNC;
    }
    if (strncmp(value, "EVENT_PERSISTED\"", 16) == 0) {
        return INBOUND_MSG_EVENT_PERSISTED;
    }
    return INBOUND_MSG_UNKNOWN;
}
