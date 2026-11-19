#!/usr/bin/env bash
# Host-side build/run for the hardware-independent core + wire_json.
# No ESP-IDF, no hardware — see docs/reports/esp/ for evidence levels.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FW_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CORE_INC="$FW_ROOT/components/core/include"
CORE_SRC="$FW_ROOT/components/core/src"
WIRE_JSON_INC="$FW_ROOT/components/wire_json/include"
WIRE_JSON_SRC="$FW_ROOT/components/wire_json/src"
CJSON_DIR="$FW_ROOT/../third_party/cJSON"
UNITY_DIR="$FW_ROOT/../third_party/Unity/src"
FAKES_DIR="$SCRIPT_DIR/fakes"
TEST_SRC_DIR="$SCRIPT_DIR/src"
BUILD_DIR="$SCRIPT_DIR/build"

# Bootstrap pinned host deps if missing (reproducible clone).
if [ ! -f "$UNITY_DIR/unity.c" ] || [ ! -f "$CJSON_DIR/cJSON.c" ]; then
    echo "Host deps missing — running tools/fetch_host_deps.sh"
    bash "$FW_ROOT/../tools/fetch_host_deps.sh"
fi

MINGW_BIN="/c/Users/Osama/AppData/Local/Microsoft/WinGet/Packages/BrechtSanders.WinLibs.POSIX.UCRT_Microsoft.Winget.Source_8wekyb3d8bbwe/mingw64/bin"
if [ -d "$MINGW_BIN" ]; then
    export PATH="$MINGW_BIN:$PATH"
fi

if ! command -v gcc >/dev/null 2>&1; then
    echo "gcc not found on PATH — cannot build host tests." >&2
    exit 1
fi

mkdir -p "$BUILD_DIR"

CORE_SOURCES=(
    "$CORE_SRC/alarm_detector.c"
    "$CORE_SRC/protection_core.c"
    "$CORE_SRC/storage_backend.c"
    "$CORE_SRC/persisted_state.c"
    "$CORE_SRC/sequence_counter.c"
    "$CORE_SRC/event_queue.c"
    "$CORE_SRC/event_queue_persist.c"
    "$CORE_SRC/ownership_state.c"
    "$CORE_SRC/command_dedup.c"
    "$CORE_SRC/remote_command.c"
    "$CORE_SRC/command_ack_builder.c"
    "$CORE_SRC/diagnostics.c"
    "$CORE_SRC/iso8601_time.c"
    "$CORE_SRC/uuid128.c"
    "$CORE_SRC/mqtt_topics.c"
    "$CORE_SRC/connection_lifecycle.c"
    "$CORE_SRC/inbound_message.c"
)

UNITY_SOURCE="$UNITY_DIR/unity.c"
if [ ! -f "$UNITY_SOURCE" ]; then
    echo "Unity not found at $UNITY_SOURCE" >&2
    exit 1
fi

TEST_FILES=(
    test_alarm_detector
    test_protection_core
    test_event_queue
    test_persisted_state
    test_sequence_counter
    test_ownership_state
    test_remote_command
    test_command_ack_builder
    test_diagnostics
    test_command_dedup
    test_iso8601_time
    test_uuid128
    test_mqtt_topics
    test_connection_lifecycle
    test_inbound_message
    test_event_queue_persist
)

WIRE_JSON_TEST_FILES=(
    test_execute_command_wire
    test_command_ack_json
    test_state_json
    test_ownership_sync_wire
    test_event_persisted_wire
    test_heartbeat_alarm_json
)

WIRE_JSON_SOURCES=(
    "$WIRE_JSON_SRC/wire_state_strings.c"
    "$WIRE_JSON_SRC/execute_command_wire.c"
    "$WIRE_JSON_SRC/command_ack_json.c"
    "$WIRE_JSON_SRC/state_json.c"
    "$WIRE_JSON_SRC/heartbeat_json.c"
    "$WIRE_JSON_SRC/alarm_event_json.c"
    "$WIRE_JSON_SRC/ownership_sync_wire.c"
    "$WIRE_JSON_SRC/event_persisted_wire.c"
    "$WIRE_JSON_SRC/event_loss_json.c"
)

FAIL_COUNT=0
TOTAL=0

for name in "${TEST_FILES[@]}"; do
    TOTAL=$((TOTAL + 1))
    out_bin="$BUILD_DIR/${name}.exe"
    echo "== building $name =="
    gcc -std=c11 -Wall -Wextra -Wno-unused-parameter \
        -I"$CORE_INC" -I"$UNITY_DIR" -I"$FAKES_DIR" \
        "$TEST_SRC_DIR/${name}.c" "$FAKES_DIR/fake_storage.c" \
        "${CORE_SOURCES[@]}" "$UNITY_SOURCE" \
        -o "$out_bin"
    echo "== running $name =="
    if ! "$out_bin"; then
        FAIL_COUNT=$((FAIL_COUNT + 1))
        echo "!! $name FAILED"
    fi
done

for name in "${WIRE_JSON_TEST_FILES[@]}"; do
    TOTAL=$((TOTAL + 1))
    out_bin="$BUILD_DIR/${name}.exe"
    echo "== building $name =="
    gcc -std=c11 -Wall -Wextra -Wno-unused-parameter \
        -I"$CORE_INC" -I"$WIRE_JSON_INC" -I"$CJSON_DIR" -I"$UNITY_DIR" -I"$FAKES_DIR" \
        "$TEST_SRC_DIR/${name}.c" "$FAKES_DIR/fake_storage.c" \
        "${CORE_SOURCES[@]}" \
        "${WIRE_JSON_SOURCES[@]}" \
        "$CJSON_DIR/cJSON.c" \
        "$UNITY_SOURCE" \
        -o "$out_bin"
    echo "== running $name =="
    if ! "$out_bin"; then
        FAIL_COUNT=$((FAIL_COUNT + 1))
        echo "!! $name FAILED"
    fi
done

echo
echo "===================================="
echo "Host test files run: $TOTAL, failed: $FAIL_COUNT"
echo "===================================="
if [ "$FAIL_COUNT" -ne 0 ]; then
    exit 1
fi
