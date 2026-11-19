#include "event_persisted_wire.h"
#include "cJSON.h"
#include <string.h>
#include <stdio.h>

static void set_err(char *err, size_t err_size, const char *msg)
{
    if (err != NULL && err_size > 0) {
        strncpy(err, msg, err_size - 1);
        err[err_size - 1] = '\0';
    }
}

bool event_persisted_parse(const char *json_text, event_persisted_wire_t *out, char *err, size_t err_size)
{
    memset(out, 0, sizeof(*out));
    cJSON *root = cJSON_Parse(json_text);
    if (root == NULL) {
        set_err(err, err_size, "invalid JSON");
        return false;
    }

    const cJSON *sv = cJSON_GetObjectItemCaseSensitive(root, "schemaVersion");
    if (sv == NULL || !cJSON_IsNumber(sv) || (int)sv->valuedouble != 1) {
        set_err(err, err_size, "schemaVersion must be 1");
        cJSON_Delete(root);
        return false;
    }

    const cJSON *mt = cJSON_GetObjectItemCaseSensitive(root, "messageType");
    if (mt == NULL || !cJSON_IsString(mt) || strcmp(mt->valuestring, "EVENT_PERSISTED") != 0) {
        set_err(err, err_size, "messageType must be EVENT_PERSISTED");
        cJSON_Delete(root);
        return false;
    }

    const cJSON *did = cJSON_GetObjectItemCaseSensitive(root, "deviceId");
    const cJSON *mid = cJSON_GetObjectItemCaseSensitive(root, "messageId");
    const cJSON *euid = cJSON_GetObjectItemCaseSensitive(root, "eventUid");
    uuid128_t tmp;
    if (did == NULL || !cJSON_IsString(did) || !uuid128_from_string(did->valuestring, &tmp)) {
        set_err(err, err_size, "missing or malformed UUID field: deviceId");
        cJSON_Delete(root);
        return false;
    }
    if (mid == NULL || !cJSON_IsString(mid) || !uuid128_from_string(mid->valuestring, &tmp)) {
        set_err(err, err_size, "missing or malformed UUID field: messageId");
        cJSON_Delete(root);
        return false;
    }
    if (euid == NULL || !cJSON_IsString(euid) || !uuid128_from_string(euid->valuestring, &out->event_uid)) {
        set_err(err, err_size, "missing or malformed UUID field: eventUid");
        cJSON_Delete(root);
        return false;
    }
    strncpy(out->device_id, did->valuestring, WIRE_UUID_LEN);
    out->device_id[WIRE_UUID_LEN] = '\0';
    strncpy(out->message_id, mid->valuestring, WIRE_UUID_LEN);
    out->message_id[WIRE_UUID_LEN] = '\0';

    const cJSON *stored = cJSON_GetObjectItemCaseSensitive(root, "stored");
    if (stored == NULL || !cJSON_IsBool(stored) || !cJSON_IsTrue(stored)) {
        set_err(err, err_size, "stored must be true");
        cJSON_Delete(root);
        return false;
    }
    out->stored = true;

    const cJSON *phase = cJSON_GetObjectItemCaseSensitive(root, "alarmPhase");
    if (phase == NULL) {
        out->has_alarm_phase = false;
        out->alarm_phase_ended = false; /* legacy omit acknowledges ACTIVE only */
    } else if (cJSON_IsString(phase) && strcmp(phase->valuestring, "ACTIVE") == 0) {
        out->has_alarm_phase = true;
        out->alarm_phase_ended = false;
    } else if (cJSON_IsString(phase) && strcmp(phase->valuestring, "ENDED") == 0) {
        out->has_alarm_phase = true;
        out->alarm_phase_ended = true;
    } else {
        set_err(err, err_size, "invalid alarmPhase");
        cJSON_Delete(root);
        return false;
    }

    cJSON_Delete(root);
    return true;
}
