#include "alarm_event_json.h"
#include "cJSON.h"

char *alarm_event_to_json(const alarm_event_wire_t *ev,
                          const char *device_id,
                          const char *message_id,
                          const char *boot_id)
{
    if (ev == NULL || device_id == NULL || message_id == NULL || boot_id == NULL || ev->sensor_key == NULL) {
        return NULL;
    }
    cJSON *root = cJSON_CreateObject();
    if (root == NULL) {
        return NULL;
    }
    char uid_str[37];
    uuid128_to_string(&ev->event_uid, uid_str);

    cJSON_AddNumberToObject(root, "schemaVersion", 1);
    cJSON_AddStringToObject(root, "deviceId", device_id);
    cJSON_AddStringToObject(root, "messageId", message_id);
    cJSON_AddStringToObject(root, "messageType", "ALARM_EVENT");
    cJSON_AddStringToObject(root, "bootId", boot_id);
    cJSON_AddStringToObject(root, "eventUid", uid_str);
    cJSON_AddNumberToObject(root, "ownershipVersion", (double)ev->ownership_version);
    cJSON_AddStringToObject(root, "sensorKey", ev->sensor_key);
    cJSON_AddBoolToObject(root, "wasBuffered", ev->was_buffered);
    cJSON_AddBoolToObject(root, "clockTrusted", ev->clock_trusted);
    cJSON_AddNullToObject(root, "occurredDeviceAt");
    if (ev->include_alarm_phase) {
        cJSON_AddStringToObject(root, "alarmPhase", ev->alarm_phase_ended ? "ENDED" : "ACTIVE");
    }

    char *text = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return text;
}
