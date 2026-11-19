# ESP-C-04: experimental draft-contract JSON (de)serialization. Depends on
# ESP-IDF's built-in "json" component (cJSON) — NOT the third_party/cJSON
# vendored separately for host tests (see tests/host/build_and_run.sh).
idf_component_register(
    SRCS
        "src/wire_state_strings.c"
        "src/execute_command_wire.c"
        "src/command_ack_json.c"
        "src/state_json.c"
        "src/heartbeat_json.c"
        "src/alarm_event_json.c"
        "src/ownership_sync_wire.c"
        "src/event_persisted_wire.c"
        "src/event_loss_json.c"
    INCLUDE_DIRS "include"
    REQUIRES core json
)
