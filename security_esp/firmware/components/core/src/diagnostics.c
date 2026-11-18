#include "diagnostics.h"

diagnostics_snapshot_t diagnostics_read(const diagnostics_config_t *config,
                                        bool measured_valid,
                                        uint8_t measured_percent,
                                        power_source_t measured_power_source)
{
    diagnostics_snapshot_t snap;
    if (!config->has_measurement_circuit || !measured_valid) {
        snap.has_battery_percent = false;
        snap.battery_percent = 0;
        snap.power_source = POWER_SOURCE_UNKNOWN;
        return snap;
    }
    snap.has_battery_percent = true;
    snap.battery_percent = measured_percent;
    snap.power_source = measured_power_source;
    return snap;
}
