#!/usr/bin/env bash
# Host-side build/run for the hardware-independent core (T-03/T-04/T-05/T-07
# pure-logic/T-08 diagnostics). No ESP-IDF, no hardware — see
# docs/reports/esp/ for what this does and does not prove.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FW_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CORE_INC="$FW_ROOT/components/core/include"
CORE_SRC="$FW_ROOT/components/core/src"
UNITY_DIR="$FW_ROOT/../third_party/Unity/src"
FAKES_DIR="$SCRIPT_DIR/fakes"
TEST_SRC_DIR="$SCRIPT_DIR/src"
BUILD_DIR="$SCRIPT_DIR/build"

# Make a locally-installed MinGW gcc visible even if the caller's shell
# hasn't picked up the PATH change yet (see docs/reports/esp/ESP-T-03-report.md).
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
    "$CORE_SRC/ownership_state.c"
    "$CORE_SRC/command_dedup.c"
    "$CORE_SRC/remote_command.c"
    "$CORE_SRC/command_ack_builder.c"
    "$CORE_SRC/diagnostics.c"
)

UNITY_SOURCE="$UNITY_DIR/unity.c"
if [ ! -f "$UNITY_SOURCE" ]; then
    echo "Unity not found at $UNITY_SOURCE (expected a vendored checkout under security_esp/third_party/Unity)." >&2
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

echo
echo "===================================="
echo "Host test files run: $TOTAL, failed: $FAIL_COUNT"
echo "===================================="
if [ "$FAIL_COUNT" -ne 0 ]; then
    exit 1
fi
