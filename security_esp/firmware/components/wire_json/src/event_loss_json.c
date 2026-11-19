#include "event_loss_json.h"
#include "cJSON.h"

char *event_loss_to_json(const char *device_id,
                         const char *message_id,
                         const char *boot_id,
                         const uuid128_t *report_uid,
                         uint64_t ownership_version,
                         bool clock_trusted)
{
    if (device_id == NULL || message_id == NULL || boot_id == NULL || report_uid == NULL) {
        return NULL;
    }
    cJSON *root = cJSON_CreateObject();
    if (root == NULL) {
        return NULL;
    }
    char uid_str[37];
    uuid128_to_string(report_uid, uid_str);
    cJSON_AddNumberToObject(root, "schemaVersion", 1);
    cJSON_AddStringToObject(root, "deviceId", device_id);
    cJSON_AddStringToObject(root, "messageId", message_id);
    cJSON_AddStringToObject(root, "messageType", "EVENT_LOSS_REPORT");
    cJSON_AddStringToObject(root, "bootId", boot_id);
    cJSON_AddStringToObject(root, "reportUid", uid_str);
    cJSON_AddNumberToObject(root, "ownershipVersion", (double)ownership_version);
    cJSON_AddStringToObject(root, "reason", "BUFFER_FULL");
    cJSON_AddBoolToObject(root, "clockTrusted", clock_trusted);
    char *text = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return text;
}
