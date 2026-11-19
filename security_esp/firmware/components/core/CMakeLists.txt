# ESP-C-02: registers the hardware-independent core (already built/unit-tested
# on the host — see tests/host/) as a normal ESP-IDF component. No ESP-IDF
# headers are included by any file here (see esp_core_types.h etc.) — this
# component must compile standalone under the Xtensa/RISC-V toolchain exactly
# like it does under host gcc.
idf_component_register(
    SRCS
        "src/alarm_detector.c"
        "src/protection_core.c"
        "src/storage_backend.c"
        "src/persisted_state.c"
        "src/sequence_counter.c"
        "src/event_queue.c"
        "src/event_queue_persist.c"
        "src/ownership_state.c"
        "src/command_dedup.c"
        "src/remote_command.c"
        "src/command_ack_builder.c"
        "src/diagnostics.c"
        "src/iso8601_time.c"
        "src/uuid128.c"
        "src/mqtt_topics.c"
        "src/connection_lifecycle.c"
        "src/inbound_message.c"
    INCLUDE_DIRS "include"
)
