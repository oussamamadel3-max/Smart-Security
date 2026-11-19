# Experimental connectivity stack (ESP-C-04 gap closure): Wi-Fi, SoftAP
# provisioning, SNTP, MQTT/TLS transport, device identity, orchestrator.
# Uses official ESP-IDF components only. Draft MQTT contracts — not G1.
idf_component_register(
    SRCS
        "src/device_identity.c"
        "src/wifi_manager.c"
        "src/wifi_provisioning_ap.c"
        "src/sntp_sync.c"
        "src/mqtt_transport.c"
        "src/conn_orchestrator.c"
    INCLUDE_DIRS "include"
    REQUIRES
        core
        wire_json
        port_esp32
        nvs_flash
        esp_wifi
        esp_netif
        esp_event
        mqtt
        esp_http_server
        esp_timer
        lwip
        freertos
)
