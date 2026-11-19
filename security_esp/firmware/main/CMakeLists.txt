idf_component_register(
    SRCS "app_main.c"
    INCLUDE_DIRS "."
    REQUIRES
        core
        port_esp32
        connectivity
        nvs_flash
        esp_hw_support
        freertos
)
