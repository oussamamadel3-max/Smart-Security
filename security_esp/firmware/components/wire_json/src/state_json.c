#include "state_json.h"
#include "wire_state_strings.h"
#include "uuid128.h"
#include "cJSON.h"

char *state_to_json(const state_snapshot_t *snap,
                    const char *device_id,
                    const char *message_id,
                    const char *boot_id)
{
    if (snap == NULL || device_id == NULL || message_id == NULL || boot_id == NULL) {
        return NULL;
    }
    cJSON *root = cJSON_CreateObject();
    if (root == NULL) {
        return NULL;
    }
    cJSON_AddNumberToObject(root, "schemaVersion", 1);
    cJSON_AddStringToObject(root, "deviceId", device_id);
    cJSON_AddStringToObject(root, "messageId", message_id);
    cJSON_AddStringToObject(root, "messageType", "STATE");
    cJSON_AddStringToObject(root, "bootId", boot_id);
    cJSON_AddNumberToObject(root, "stateSequence", (double)snap->state_sequence);
    cJSON_AddNumberToObject(root, "confirmedOwnershipVersion", (double)snap->confirmed_ownership_version);
    cJSON_AddStringToObject(root, "armState", wire_arm_state_str(snap->arm_state));
    cJSON_AddStringToObject(root, "sirenState", wire_siren_state_str(snap->siren_state));
    cJSON_AddStringToObject(root, "healthState", wire_health_state_str(snap->health_state));
    if (snap->has_current_alarm_event_uid) {
        char uid_str[37];
        uuid128_to_string(&snap->current_alarm_event_uid, uid_str);
        cJSON_AddStringToObject(root, "currentAlarmEventUid", uid_str);
    } else {
        cJSON_AddNullToObject(root, "currentAlarmEventUid");
    }
    cJSON_AddBoolToObject(root, "clockTrusted", snap->clock_trusted);

    char *text = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return text;
}
