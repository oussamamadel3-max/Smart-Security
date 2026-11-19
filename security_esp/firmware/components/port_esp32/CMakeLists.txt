# ESP-C-03: binds the hardware-independent `core` component to real ESP-IDF
# APIs (NVS, GPIO, esp_timer, hardware RNG). This is the one component in
# the project allowed to include ESP-IDF headers directly.
idf_component_register(
    SRCS
        "src/nvs_storage_backend.c"
        "src/hw_rng.c"
        "src/sensor_siren_port.c"
    INCLUDE_DIRS "include"
    REQUIRES core nvs_flash esp_driver_gpio esp_timer esp_hw_support
)
