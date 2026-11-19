#include "heartbeat_json.h"
#include "cJSON.h"

char *heartbeat_to_json(const char *device_id,
                        const char *message_id,
                        const char *boot_id,
                        bool clock_trusted)
{
    if (device_id == NULL || message_id == NULL || boot_id == NULL) {
        return NULL;
    }
    cJSON *root = cJSON_CreateObject();
    if (root == NULL) {
        return NULL;
    }
    cJSON_AddNumberToObject(root, "schemaVersion", 1);
    cJSON_AddStringToObject(root, "deviceId", device_id);
    cJSON_AddStringToObject(root, "messageId", message_id);
    cJSON_AddStringToObject(root, "messageType", "HEARTBEAT");
    cJSON_AddStringToObject(root, "bootId", boot_id);
    cJSON_AddBoolToObject(root, "clockTrusted", clock_trusted);
    char *text = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return text;
}
