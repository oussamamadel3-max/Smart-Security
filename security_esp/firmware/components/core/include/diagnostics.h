/*
 * T-08 — "لا تخترع قياسات البطارية" / "دعم UNKNOWN عند عدم توفر القياس".
 * Pure gatekeeper: when the device has no approved measurement circuit, this
 * module reports UNKNOWN/absent regardless of whatever raw value a caller
 * might otherwise have on hand. It never guesses a battery percentage.
 */
#ifndef DIAGNOSTICS_H
#define DIAGNOSTICS_H

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
    POWER_SOURCE_UNKNOWN = 0,
    POWER_SOURCE_MAINS,
    POWER_SOURCE_BATTERY
} power_source_t;

typedef struct {
    /* Set true only once a specific, calibrated voltage-sensing circuit is
     * adopted in D-01/T-02. Defaults to false (no measurement). */
    bool has_measurement_circuit;
} diagnostics_config_t;

typedef struct {
    bool has_battery_percent;
    uint8_t battery_percent; /* valid only if has_battery_percent */
    power_source_t power_source;
} diagnostics_snapshot_t;

diagnostics_snapshot_t diagnostics_read(const diagnostics_config_t *config,
                                        bool measured_valid,
                                        uint8_t measured_percent,
                                        power_source_t measured_power_source);

#ifdef __cplusplus
}
#endif

#endif /* DIAGNOSTICS_H */
