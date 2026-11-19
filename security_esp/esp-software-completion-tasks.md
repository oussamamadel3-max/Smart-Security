# مهام استكمال برمجي ESP — ESP-C-01…ESP-C-06

آخر تحديث: 2026-11-19 (إعادة فتح C-03…C-06 وفق معايير القبول الأصلية ثم إغلاق ذاتي بعد سد الفجوات البرمجية)

**عدّاد مستقل: 6/6 `COMPLETED` — لا يُضاف لعدّاد ESP الأصلي (9) ولا للعدّاد الرسمي للنظام.**

## ESP-C-01 — تجهيز بيئة ESP-IDF

- **الحالة:** `COMPLETED`
- **الدليل:** ESP-IDF `v5.3.6` / commit `db32f7de6368445b431361f30e72be38ba2994d3` موثَّق في `tools/IDF_VERSION.lock`.

## ESP-C-02 — هيكل Firmware

- **الحالة:** `COMPLETED`
- **الدليل:** مشروع IDF يبني `core` + `wire_json` + `port_esp32` + `connectivity` + `main`.

## ESP-C-03 — ربط النواة مع ESP-IDF

- **الحالة:** أُعيد فتحها ثم `COMPLETED` (برمجياً)
- **ما أُكمل إضافياً 2026-11-19:** `app_main` يستعيد `persisted_state` + `sequence_counter` + `event_queue_persist`، يبدأ `sensor_siren_port` قبل الشبكة، ثم `conn_orchestrator_start`. استقلال الحماية المحلية عن الشبكة محفوظ بالترتيب.
- **متبقي عتاد:** معايرة GPIO/AC-T-01/02.

## ESP-C-04 — طبقة الاتصال والتكامل التجريبي

- **الحالة:** كانت `PARTIALLY IMPLEMENTED` → أُعيد فتحها → `COMPLETED` (برمجياً / تجريبي)
- **المكوّنات المنفَّذة والمربوطة:**
  - Wi-Fi Manager (`wifi_manager`)
  - MQTT Client + TLS اختياري (`mqtt_transport` — `mqtts://` يرفض بلا CA؛ لا مصادقة إنتاج مخترَعة)
  - Device Identity (`device_identity` في NVS)
  - SNTP (`sntp_sync` → `clockTrusted` فقط بعد مزامنة)
  - Connection lifecycle + automatic reconnect (دورة حياة + `esp_wifi_connect` عند الفصل)
  - State / Event / ACK publishing + Command receiving
  - Ownership sync + EVENT_PERSISTED handling
  - Local SoftAP provisioning (يرفض ARM/DISARM صراحةً)
- **العقود:** مسودة `docs/contracts/mqtt/v1` فقط — ليست اعتماد G1.
- **متبقي:** اختبار حي على عتاد/وسيط + اعتماد G1 للإنتاج.

## ESP-C-05 — فجوات وجودة واختبارات

- **الحالة:** أُعيد فتحها ثم `COMPLETED` للنطاق البرمجي
- **الإضافات:** استمرارية طابور الأحداث، دورة حياة الاتصال، تصنيف الوارد، اختبارات جديدة؛ إجمالي المضيف **100 اختبار / 22 ملف / 0 فشل**.
- **خطأ حقيقي مُصلَح:** checksum لـ`event_queue_persist` كان يستخدم `sizeof-4` ويتأثر بـpadding بعد `checksum` — صُحِّح بـ`offsetof`.
- **متبقي:** مراجعة Watchdog/حمل FreeRTOS على عتاد حقيقي.

## ESP-C-06 — بناء نهائي وتوثيق

- **الحالة:** أُعيد فتحها ثم `COMPLETED`
- **الدليل الفعلي 2026-11-19:** `idf.py fullclean` + `idf.py build` → **1018/1018**، `Project build complete`، exit 0، **0 أخطاء مترجم**. تحذير Kconfig وحيد سابقاً لرمز غير موجود أُزيل من `sdkconfig.defaults`. الناتج: `smart_security_esp.bin` = `0xea740` بايت (960320) ≈ 8% فراغ في قسم 1MB.
- **اعتماديات قابلة لإعادة البناء:** `tools/fetch_host_deps.sh` + `third_party/HOST_DEPS.lock` + `tools/IDF_VERSION.lock` + `security_esp/.gitignore`.
- **لا flash.**

---

**عدّاد الاستكمال: 6/6. العدّاد الرسمي ESP: 0/9 بلا تغيير.**
