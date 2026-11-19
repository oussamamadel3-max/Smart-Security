# تقرير إغلاق فجوة — ESP-C-04 (إعادة فتح ثم إغلاق)

التاريخ: 2026-11-19

أُعيد فتح ESP-C-04 لأن القبول الأصلي طلب طبقة اتصال تجريبية كاملة، بينما الجولة السابقة توقفت عند `wire_json` فقط.

## ما أُنجز

مكوّن `firmware/components/connectivity/`: Wi-Fi Manager، MQTT Client، TLS اختياري، Device Identity، SNTP، Connection/Reconnect، نشر STATE/أحداث/ACK، استقبال أوامر، OWNERSHIP_SYNC، EVENT_PERSISTED، SoftAP provisioning — مربوط عبر `conn_orchestrator` من `app_main`.

## الدليل

- Host: ضمن إجمالي 100 اختبار (يشمل ownership/event_persisted/state/topics/lifecycle).
- `idf.py build` بعد fullclean: ناجح (1018/1018).
- لا اختبار شبكة على عتاد حقيقي.

## الإغلاق

`COMPLETED` للنطاق البرمجي التجريبي. القبول الحي وG1 يبقيان في `ESP-REMAINING-WORK.md`.
