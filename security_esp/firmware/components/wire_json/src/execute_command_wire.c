#include "execute_command_wire.h"
#include "iso8601_time.h"
#include "uuid128.h"
#include "cJSON.h"
#include <string.h>
#include <ctype.h>
#include <stdio.h>

static void set_err(char *err, size_t err_size, const char *msg)
{
    if (err != NULL && err_size > 0) {
        strncpy(err, msg, err_size - 1);
        err[err_size - 1] = '\0';
    }
}

static bool is_valid_uuid(const char *s)
{
    if (s == NULL || strlen(s) != WIRE_UUID_LEN) {
        return false;
    }
    static const int dash_positions[] = { 8, 13, 18, 23 };
    for (size_t i = 0; i < sizeof(dash_positions) / sizeof(dash_positions[0]); i++) {
        if (s[dash_positions[i]] != '-') {
            return false;
        }
    }
    for (int i = 0; i < WIRE_UUID_LEN; i++) {
        bool is_dash_pos = (i == 8 || i == 13 || i == 18 || i == 23);
        if (is_dash_pos) {
            continue;
        }
        if (!isxdigit((unsigned char)s[i])) {
            return false;
        }
    }
    return true;
}

static const char *get_required_string(const cJSON *root, const char *key)
{
    const cJSON *item = cJSON_GetObjectItemCaseSensitive(root, key);
    if (item == NULL || !cJSON_IsString(item) || item->valuestring == NULL) {
        return NULL;
    }
    return item->valuestring;
}

static bool get_required_uuid(const cJSON *root, const char *key, char *out, char *err, size_t err_size)
{
    const char *s = get_required_string(root, key);
    if (s == NULL || !is_valid_uuid(s)) {
        char msg[64];
        snprintf(msg, sizeof(msg), "missing or malformed UUID field: %s", key);
        set_err(err, err_size, msg);
        return false;
    }
    strncpy(out, s, WIRE_UUID_LEN);
    out[WIRE_UUID_LEN] = '\0';
    return true;
}

static bool get_required_uuid128(const cJSON *root, const char *key, uuid128_t *out, char *err, size_t err_size)
{
    const char *s = get_required_string(root, key);
    if (s == NULL || !uuid128_from_string(s, out)) {
        char msg[64];
        snprintf(msg, sizeof(msg), "missing or malformed UUID field: %s", key);
        set_err(err, err_size, msg);
        return false;
    }
    return true;
}

static bool get_required_uint(const cJSON *root, const char *key, uint64_t *out, char *err, size_t err_size)
{
    const cJSON *item = cJSON_GetObjectItemCaseSensitive(root, key);
    if (item == NULL || !cJSON_IsNumber(item) || item->valuedouble < 0) {
        char msg[64];
        snprintf(msg, sizeof(msg), "missing or invalid integer field: %s", key);
        set_err(err, err_size, msg);
        return false;
    }
    *out = (uint64_t)item->valuedouble;
    return true;
}

static bool parse_type(const char *s, remote_command_type_t *out)
{
    if (strcmp(s, "ARM") == 0) { *out = REMOTE_COMMAND_TYPE_ARM; return true; }
    if (strcmp(s, "DISARM") == 0) { *out = REMOTE_COMMAND_TYPE_DISARM; return true; }
    if (strcmp(s, "MUTE_ALARM") == 0) { *out = REMOTE_COMMAND_TYPE_MUTE_ALARM; return true; }
    if (strcmp(s, "TEST_SIREN") == 0) { *out = REMOTE_COMMAND_TYPE_TEST_SIREN; return true; }
    if (strcmp(s, "STOP_TEST") == 0) { *out = REMOTE_COMMAND_TYPE_STOP_TEST; return true; }
    return false;
}

bool execute_command_parse(const char *json_text, execute_command_wire_t *out, char *err, size_t err_size)
{
    memset(out, 0, sizeof(*out));

    cJSON *root = cJSON_Parse(json_text);
    if (root == NULL) {
        set_err(err, err_size, "invalid JSON");
        return false;
    }

    bool ok = false;
    do {
        const cJSON *schema_version = cJSON_GetObjectItemCaseSensitive(root, "schemaVersion");
        if (schema_version == NULL || !cJSON_IsNumber(schema_version) || (int)schema_version->valuedouble != 1) {
            set_err(err, err_size, "schemaVersion must be exactly 1");
            break;
        }

        const char *message_type = get_required_string(root, "messageType");
        if (message_type == NULL || strcmp(message_type, "EXECUTE_COMMAND") != 0) {
            set_err(err, err_size, "messageType must be EXECUTE_COMMAND");
            break;
        }

        if (!get_required_uuid(root, "deviceId", out->device_id, err, err_size)) break;
        if (!get_required_uuid(root, "messageId", out->message_id, err, err_size)) break;
        if (!get_required_uuid(root, "commandId", out->command_id, err, err_size)) break;
        if (!get_required_uint(root, "commandSequence", &out->command_sequence, err, err_size)) break;
        if (!get_required_uint(root, "ownershipVersion", &out->ownership_version, err, err_size)) break;

        const char *type_str = get_required_string(root, "type");
        if (type_str == NULL || !parse_type(type_str, &out->type)) {
            set_err(err, err_size, "type must be one of ARM/DISARM/MUTE_ALARM/TEST_SIREN/STOP_TEST");
            break;
        }

        const char *expires_at = get_required_string(root, "expiresAt");
        if (expires_at == NULL || !iso8601_utc_to_epoch_ms(expires_at, &out->expires_at_epoch_ms)) {
            set_err(err, err_size, "expiresAt missing or not yyyy-MM-ddTHH:mm:ssZ");
            break;
        }

        if (out->type == REMOTE_COMMAND_TYPE_MUTE_ALARM) {
            if (!get_required_uuid128(root, "targetAlarmEventUid", &out->target_alarm_event_uid, err, err_size)) {
                break;
            }
            out->has_target_event = true;
        }

        ok = true;
    } while (0);

    cJSON_Delete(root);
    return ok;
}

void execute_command_wire_to_core(const execute_command_wire_t *wire, execute_command_t *core_cmd)
{
    memset(core_cmd, 0, sizeof(*core_cmd));
    strncpy(core_cmd->command_id, wire->command_id, sizeof(core_cmd->command_id) - 1);
    core_cmd->command_sequence = wire->command_sequence;
    core_cmd->ownership_version = wire->ownership_version;
    core_cmd->type = wire->type;
    core_cmd->expires_at_ms = (esp_core_time_ms_t)wire->expires_at_epoch_ms;
    core_cmd->has_target_event = wire->has_target_event;
    core_cmd->target_event_uid = wire->target_alarm_event_uid;
}
