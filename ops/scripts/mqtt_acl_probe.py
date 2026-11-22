#!/usr/bin/env python3
"""OPS-T-01 ACL probe using MQTT v5 reason codes (mosquitto_pub hides PUBACK 135)."""

from __future__ import annotations

import argparse
import os
import sys
import time

import paho.mqtt.client as mqtt


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True)
    parser.add_argument("--port", type=int, default=1883)
    parser.add_argument("--username", default=os.environ.get("MQTT_USERNAME") or None)
    parser.add_argument("--password", default=os.environ.get("MQTT_PASSWORD") or "")
    parser.add_argument("--topic", required=True)
    parser.add_argument("--message", default="probe")
    parser.add_argument("--action", choices=["publish", "subscribe"], required=True)
    parser.add_argument("--wait", type=float, default=3.0)
    parser.add_argument("--cafile")
    args = parser.parse_args()

    result = {"connack": None, "puback": None, "payload": None, "error": None}

    client = mqtt.Client(
        callback_api_version=mqtt.CallbackAPIVersion.VERSION2,
        client_id="",
        protocol=mqtt.MQTTv5,
    )
    if args.username:
        client.username_pw_set(args.username, args.password)
    if args.cafile:
        client.tls_set(ca_certs=args.cafile)

    def on_connect(client, userdata, flags, reason_code, properties):
        result["connack"] = int(reason_code.value if hasattr(reason_code, "value") else reason_code)
        if args.action == "subscribe" and result["connack"] == 0:
            client.subscribe(args.topic, qos=1)

    def on_publish(client, userdata, mid, reason_code, properties):
        result["puback"] = int(reason_code.value if hasattr(reason_code, "value") else reason_code)

    def on_message(client, userdata, msg):
        result["payload"] = msg.payload.decode("utf-8", errors="replace")

    client.on_connect = on_connect
    client.on_publish = on_publish
    client.on_message = on_message

    try:
        client.connect(args.host, args.port, keepalive=30)
    except Exception as ex:  # noqa: BLE001 - probe tool
        print(f"CONNECT_ERROR {ex}")
        return 2

    client.loop_start()
    deadline = time.time() + args.wait
    while time.time() < deadline and result["connack"] is None:
        time.sleep(0.05)

    if result["connack"] is None:
        print("CONNACK_TIMEOUT")
        client.loop_stop()
        return 3
    if result["connack"] != 0:
        print(f"CONNACK {result['connack']}")
        client.loop_stop()
        return 1

    if args.action == "publish":
        info = client.publish(args.topic, args.message, qos=1)
        while time.time() < deadline and result["puback"] is None:
            time.sleep(0.05)
        client.loop_stop()
        if result["puback"] is None:
            # Fallback to paho mid rc if callback missed
            try:
                info.wait_for_publish(timeout=1.0)
                print(f"PUBLISH_FALLBACK rc={info.rc}")
                return 0 if info.rc == 0 else 1
            except Exception as ex:  # noqa: BLE001
                print(f"PUBACK_TIMEOUT {ex}")
                return 4
        # MQTT5: 0 = success, 16 = no matching subscribers (still authorized).
        # 135 = not authorized.
        print(f"PUBACK {result['puback']}")
        return 0 if result["puback"] in (0, 16) else 1

    # subscribe: wait for optional message
    while time.time() < deadline and result["payload"] is None:
        time.sleep(0.05)
    client.loop_stop()
    if result["payload"] is None:
        print("NO_MESSAGE")
        return 1
    print(f"MESSAGE {result['payload']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
