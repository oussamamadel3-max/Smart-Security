#include "unity.h"
#include "diagnostics.h"

void setUp(void) {}
void tearDown(void) {}

static void test_no_measurement_circuit_always_unknown(void)
{
    diagnostics_config_t cfg = { .has_measurement_circuit = false };
    /* Even if a caller somehow has a "valid" raw reading, without an approved
     * circuit it must never surface as a real battery percentage. */
    diagnostics_snapshot_t snap = diagnostics_read(&cfg, true, 80, POWER_SOURCE_BATTERY);
    TEST_ASSERT_FALSE(snap.has_battery_percent);
    TEST_ASSERT_EQUAL(POWER_SOURCE_UNKNOWN, snap.power_source);
}

static void test_measurement_circuit_present_but_reading_invalid_is_unknown(void)
{
    diagnostics_config_t cfg = { .has_measurement_circuit = true };
    diagnostics_snapshot_t snap = diagnostics_read(&cfg, false, 80, POWER_SOURCE_BATTERY);
    TEST_ASSERT_FALSE(snap.has_battery_percent);
}

static void test_measurement_circuit_present_and_valid_passes_through(void)
{
    diagnostics_config_t cfg = { .has_measurement_circuit = true };
    diagnostics_snapshot_t snap = diagnostics_read(&cfg, true, 55, POWER_SOURCE_MAINS);
    TEST_ASSERT_TRUE(snap.has_battery_percent);
    TEST_ASSERT_EQUAL(55, snap.battery_percent);
    TEST_ASSERT_EQUAL(POWER_SOURCE_MAINS, snap.power_source);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_no_measurement_circuit_always_unknown);
    RUN_TEST(test_measurement_circuit_present_but_reading_invalid_is_unknown);
    RUN_TEST(test_measurement_circuit_present_and_valid_passes_through);
    return UNITY_END();
}
