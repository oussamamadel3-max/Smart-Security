/*
 * ESP-C-03 — binds protection_core.h (already unit-tested, hardware-
 * independent) to real GPIO on ESP32, via periodic polling rather than a
 * GPIO interrupt. Chosen deliberately over an ISR-based edge counter:
 * protection_core_on_sensor_pulse()/_tick() are ordinary functions (heap
 * use, no IRAM_ATTR, no ISR-safety audit done), so calling them directly
 * from a GPIO ISR would be unsafe. A short esp_timer period
 * (poll_interval_ms) gives debounce-grade resolution without that risk —
 * alarm_detector.h's own debounce window (D-02, still uncalibrated)
 * operates on whatever granularity this polling period provides.
 *
 * GPIO pin numbers in sensor_siren_port_config_t are NOT approved hardware
 * decisions — D-01 (board/circuit) is still open (see
 * security_esp/docs/HARDWARE-DECISIONS-D01-D07.md). Callers must supply
 * real pin numbers once D-01 closes; nothing here hard-codes a guess.
 *
 * Active-high assumption for both sensor input and siren output is also a
 * placeholder pending D-01's actual SW-420/driver-circuit polarity — flip
 * invert_sensor_level / invert_siren_level if the approved circuit is
 * active-low.
 */
#ifndef SENSOR_SIREN_PORT_H
#define SENSOR_SIREN_PORT_H

#include "protection_core.h"
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    int sensor_gpio;            /* SW-420 digital output pin — placeholder, see header comment */
    int siren_gpio;              /* siren driver (relay/MOSFET) control pin — placeholder */
    bool invert_sensor_level;    /* true if the approved circuit is active-low */
    bool invert_siren_level;     /* true if the approved driver circuit is active-low */
    uint32_t poll_interval_ms;   /* e.g. 10ms; not a D-03-approved production value */
} sensor_siren_port_config_t;

/* Configures GPIO, starts the periodic esp_timer, and begins driving
 * `core` (ARM/DISARM must still be called by the command-handling layer —
 * this only feeds sensor pulses in and drives the siren output out).
 * `core` must already be initialized (protection_core_init) and must
 * outlive the port (it is accessed from the timer callback by pointer).
 * Returns false on any ESP-IDF API failure (GPIO config, timer create). */
bool sensor_siren_port_start(const sensor_siren_port_config_t *config, protection_core_t *core);

/* Stops the polling timer. Does not reset GPIO configuration. */
void sensor_siren_port_stop(void);

#ifdef __cplusplus
}
#endif

#endif /* SENSOR_SIREN_PORT_H */
