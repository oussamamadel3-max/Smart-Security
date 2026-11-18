# بناء `security_esp/firmware` — دليل مؤقت

آخر تحديث: 2026-11-18

## حالة هذه الوثيقة

لا توجد بيئة ESP-IDF ولا عتاد ESP32 فعلي في بيئة التطوير التي كُتب فيها هذا الكود (تحقّق فعلي: لا `idf.py`، لا Python، لا CMake/Ninja مثبتة). ما يلي هو **دليل بناء مستهدف** لتهيئة لاحقة على بيئة تملك الأدوات، ليس تأكيداً لبناء تم فعلاً بـESP-IDF.

## بنية المصدر

```
firmware/
  components/core/        # منطق مستقل عن العتاد والشبكة (C11 صرف، يُبنى بأي مترجم C)
    include/*.h
    src/*.c
  tests/host/              # اختبارات Unity تُبنى وتُشغَّل على الجهاز المضيف (Windows/gcc هنا)
    src/test_*.c
    fakes/fake_storage.c
    build_and_run.sh        # ينفَّذ فعلياً في هذه الجلسة (gcc من WinLibs عبر winget)، لا يحتاج ESP-IDF
  main/                     # غير موجود بعد — يُضاف عند دمج components/core مع ESP-IDF الفعلي (T-06+)
```

`components/core` لا يتضمن أي استدعاء GPIO/Wi-Fi/MQTT/NVS مباشر — فقط واجهات (`storage_backend_t`) يطبّقها منفذ ESP-IDF فعلي لاحقاً (مكوّن `components/port_esp32/` مستقبلي، غير مكتوب في هذه الجولة لعدم توفر ESP-IDF للتحقق منه).

## تشغيل اختبارات المضيف (منفَّذ فعلياً في هذه الجلسة)

```bash
cd security_esp/firmware/tests/host
bash build_and_run.sh
```

النتيجة الفعلية في هذه الجلسة: **10 ملفات اختبار، 53 حالة اختبار Unity، 0 فشل** (راجع `docs/reports/esp/` لتفاصيل كل مهمة). يستخدم السكربت مترجم GCC من حزمة `BrechtSanders.WinLibs.POSIX.UCRT` (مثبّتة عبر `winget` في هذه الجلسة بتفويض مسبق من المالك لتثبيت اعتماديات التطوير)، ومكتبة [Unity](https://github.com/ThrowTheSwitch/Unity) (مستنسخة محلياً بإصدار مثبّت `v2.6.1` في `security_esp/third_party/Unity`، غير مضافة إلى Git حتى الآن).

## البناء المستهدف على ESP-IDF (لم يُنفَّذ، يتطلب بيئة حقيقية)

- إصدار ESP-IDF المستهدف: **v5.3.x (LTS)** — مقترح للاستقرار طويل الأمد؛ **لم يُثبَّت ولم يُعتمد فعلياً**، يحتاج تأكيد المالك عند توفر بيئة البناء.
- عند التثبيت الفعلي: إضافة `main/CMakeLists.txt` يضم `components/core` كمكوّن، وإنشاء `components/port_esp32/` يطبّق `storage_backend_t` عبر NVS الحقيقي، ويقود GPIO الحساس/الصفارة.
- لا تفعيل لـSecure Boot أو Flash/NVS Encryption في هذه المرحلة (`T-09`، مؤجل حتى مراجعة منفصلة قبل أجهزة الإنتاج).

## لماذا لا يوجد `main/app_main.c` بعد

ربط `components/core` بحلقة تشغيل ESP-IDF فعلية (قراءة GPIO، مؤقتات FreeRTOS، esp_wifi، esp_mqtt) يتطلب ESP-IDF نفسه للتحقق من الربط (لا قيمة في كتابة كود لا يمكن بناؤه أو اختباره في هذه البيئة) — مسجَّل في `ESP-REMAINING-WORK.md`.
