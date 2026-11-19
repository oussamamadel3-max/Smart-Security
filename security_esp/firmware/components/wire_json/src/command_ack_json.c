#include "command_ack_json.h"
#include "wire_state_strings.h"
#include "cJSON.h"

char *command_ack_to_json(const command_ack_t *ack,
                          const char *device_id,
                          const char *message_id,
                          const char *boot_id,
                          const char *command_id)
{
    cJSON *root = cJSON_CreateObject();
    if (root == NULL) {
        return NULL;
    }

    cJSON_AddNumberToObject(root, "schemaVersion", 1);
    cJSON_AddStringToObject(root, "deviceId", device_id);
    cJSON_AddStringToObject(root, "messageId", message_id);
    cJSON_AddStringToObject(root, "messageType", "COMMAND_ACK");
    cJSON_AddStringToObject(root, "bootId", boot_id);
    cJSON_AddStringToObject(root, "commandId", command_id);
    cJSON_AddNumberToObject(root, "ownershipVersion", (double)ack->ownership_version);
    cJSON_AddStringToObject(root, "status", ack->executed ? "EXECUTED" : "REJECTED");
    cJSON_AddNumberToObject(root, "resultStateSequence", (double)ack->result_state_sequence);
    cJSON_AddStringToObject(root, "armState", wire_arm_state_str(ack->arm_state));
    cJSON_AddStringToObject(root, "sirenState", wire_siren_state_str(ack->siren_state));
    cJSON_AddStringToObject(root, "healthState", wire_health_state_str(ack->health_state));
    if (ack->has_current_alarm_event_uid) {
        char uid_str[37];
        uuid128_to_string(&ack->current_alarm_event_uid, uid_str);
        cJSON_AddStringToObject(root, "currentAlarmEventUid", uid_str);
    } else {
        cJSON_AddNullToObject(root, "currentAlarmEventUid");
    }
    cJSON_AddNumberToObject(root, "confirmedOwnershipVersion", (double)ack->confirmed_ownership_version);
    cJSON_AddBoolToObject(root, "clockTrusted", ack->clock_trusted);
    if (!ack->executed && ack->reject_code != NULL) {
        cJSON_AddStringToObject(root, "rejectCode", ack->reject_code);
    }

    char *text = cJSON_PrintUnformatted(root);
    cJSON_Delete(root);
    return text;
}
