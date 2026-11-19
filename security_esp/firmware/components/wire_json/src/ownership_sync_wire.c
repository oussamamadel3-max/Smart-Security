#include "ownership_sync_wire.h"
#include "uuid128.h"
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

bool ownership_sync_parse(const char *json_text, ownership_sync_wire_t *out, char *err, size_t err_size)
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
    if (mt == NULL || !cJSON_IsString(mt) || strcmp(mt->valuestring, "OWNERSHIP_SYNC") != 0) {
        set_err(err, err_size, "messageType must be OWNERSHIP_SYNC");
        cJSON_Delete(root);
        return false;
    }

    const cJSON *did = cJSON_GetObjectItemCaseSensitive(root, "deviceId");
    const cJSON *mid = cJSON_GetObjectItemCaseSensitive(root, "messageId");
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
    strncpy(out->device_id, did->valuestring, WIRE_UUID_LEN);
    out->device_id[WIRE_UUID_LEN] = '\0';
    strncpy(out->message_id, mid->valuestring, WIRE_UUID_LEN);
    out->message_id[WIRE_UUID_LEN] = '\0';

    const cJSON *ov = cJSON_GetObjectItemCaseSensitive(root, "ownershipVersion");
    if (ov == NULL || !cJSON_IsNumber(ov)) {
        set_err(err, err_size, "missing ownershipVersion");
        cJSON_Delete(root);
        return false;
    }
    out->ownership_version = (uint64_t)ov->valuedouble;

    const cJSON *linked = cJSON_GetObjectItemCaseSensitive(root, "isLinked");
    if (linked == NULL || !cJSON_IsBool(linked)) {
        set_err(err, err_size, "missing isLinked");
        cJSON_Delete(root);
        return false;
    }
    out->is_linked = cJSON_IsTrue(linked);

    cJSON_Delete(root);
    return true;
}
