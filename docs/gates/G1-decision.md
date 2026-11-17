# قرار بوابة G1 — محدَّث 2026-11-17

**الحالة: `G1 BLOCKED`.**

ليست `G1 APPROVED` وليست `G1 READY FOR APPROVAL`. دليل 2026-11-16 في `docs/gates/G1-evidence.md` يضم الآن اتصال TLS اختبارياً واستعادة QoS 1 على وسيط persistence. ذلك لا يغلق البوابة.

## لماذا BLOCKED

بند واحد غير مثبت يكفي للإبقاء على الحجب. TLS نجح على CA اختبار في `%TEMP%`، والإنتاج يبقى `UseTls=false` و`LivePublishEnabled=false`. لا عتاد لإثبات التسلسل عبر إعادة تشغيل ESP32. لا تنفيذ ESP/Flutter على العقد المعتمد. `D-03` / `D-05` / `NFR-008` تبقى `OPEN` رغم اعتماد القيم الابتدائية للاختبار. persistence على Mosquitto ليست التخزين المحلي المطلوب على ESP32. استثناء `IOException` المحقون ليس قتل PostgreSQL.

ما ثبت ولا يُعمَّم:

- عميل MQTT تجريبي على Mosquitto `2.1.2`: ACL النصي أُعيد 7/7 في 14 m 10 s بعد فشل عابر واحد أُعيد تشغيله.
- Backend والوسيط وPostgreSQL: `LiveMqttInboundIntegrationTests` أُعيد 7/7 في 17 m 17 s. `LiveMqttTlsReliabilityTests` نجح 8/8 في 4.8723 Minutes: TLS مع التحقق، رفض CA غير موثوق، ACL فوق TLS، استعادة QoS 1 بعد انقطاع المستهلك وإعادة تشغيل الوسيط، إعادة محاولة Outbox مع بقاء الأمر غير منفَّذ، وعدم نشر الأمر المنتهي.
- مشترك الاختبار ليس ESP32. `RecordingMqttOutboundPublisher` لم يُستخدم في هذا الإثبات.

## محسوم تعاقدياً في المرجع والكود الحالي

هذه نقاط لا تُعاد فتحها هنا، وهي منفذة كمسودة لا كاعتماد G1:

- مصدر الحقيقة هو آخر حالة أكدها الجهاز. HTTP 202 و`PENDING` وMQTT PUBACK لا يغيرون الحماية.
- `COMMAND_ACK` يحمل `resultStateSequence` ولقطة `armState` / `sirenState` / `healthState`. التسلسل الأقدم لا يرجّع `device_states`.
- القنوات: `devices/{deviceId}/state|events|acks|commands`.
- `EVENT_PERSISTED.stored=true` بعد التزام الحفظ فقط.
- `OWNERSHIP_SYNC` بلا هوية مالك. الربط الأولي `PENDING` حتى إقرار الإصدار. النقل يفتح `ACTIVE` على الخادم فوراً ويحتجز النشر حتى الإقرار.
- إنشاء الأمر: `IdempotencyKey` و`202` و`PENDING`.

## يحتاج إثباتاً

- QoS 1 ومنع retained على أمر لحظي: مثبت. استعادة `STATE` بالتسلسل 5 أثناء انقطاع المستهلك مثبتة على وسيط الاختبار مع persistence. إعادة تشغيل ESP32 غير مثبتة.
- ACL بهويتين `deva`/`devb` وخادم `srv`: مثبت نصياً وفوق TLS في الاختبار. شهادة بيئة الإنتاج غير معتمدة، و`UseTls` في `appsettings.json` يبقى `false`.
- اشتراك وارد حي لـ `COMMAND_ACK` / `STATE` / `HEARTBEAT` / `ALARM_EVENT` / `EVENT_LOSS_REPORT` / `DEVICE_FAULT`: مثبت في الاختبار عند تفعيل `Mqtt:Inbound:Enabled`. الإنتاج يبقى غير مفعّل في `appsettings.json`.
- استمرار `stateSequence` بعد إعادة تشغيل ESP32، ورفض الأمر عندما لا يستطيع الجهاز التحقق من `expiresAt`.
- عدم حذف الحدث على الجهاز قبل `EVENT_PERSISTED` عند تعطل القاعدة.
- تمييز Flutter/Web بين `PENDING` والحالة المؤكدة وبين ملكية `PENDING` و`ACTIVE`.

## قرارات العقد المعتمدة — ليست اعتماد G1

التفصيل في `docs/gates/G1-contract-decisions.md`. وُوفق على الثمانية في 2026-11-16 ونُفذت على المسودة والخادم. لم تُغلق البوابة.

1. `alarmPhase` على `ALARM_EVENT` و`EVENT_PERSISTED`: `ACTIVE` أو `ENDED`، نفس `eventUid`. الغياب يعني `ACTIVE`. الكتم لا ينهي الحادثة.
2. `SETTINGS_SYNC` وOTA مؤجلان خارج v1. لا schema.
3. `confirmedOwnershipVersion` و`clockTrusted` اختياريان في `COMMAND_ACK` v1. الغياب ليس إثبات ملكية ولا ثقة ساعة. Firmware الجديد يرسلهما.
4. الأمر البعيد لا يُنفَّذ على الجهاز إذا تعذر التحقق من `expiresAt`. الحماية المحلية تستمر. هذه قاعدة ESP ولم يُبنَ firmware.
5. 30 ثانية و60 دقيقة و10 ثوانٍ وتغطية ساعة انقطاع قيم اختبار ابتدائية. السعة العددية وTTL تبقى `OPEN`.
6. استمرار `stateSequence` بعد إعادة التشغيل مبدأ معتمد. NVS وحجز النطاقات دراسة، لا تصميم مجمّد قبل اختبار انقطاع الطاقة.
7. MQTT الإنتاج يبقى متوقفاً. لا اتصال إنتاجي غير مشفر. `NoOp` لا يعلّم صف Outbox منشوراً.

## أصغر خطوات قبل READY FOR APPROVAL

1. القرارات الثمانية مكتوبة ومنفذة على المسودة والخادم. ما تبقى قبل الجاهزية ليس إعادة فتحها.
2. اعتماد شهادة بيئة الإنتاج أو استثناء TLS بقرار مكتوب. نجاح CA الاختبار لا يغيّر `appsettings.json`.
3. اختبار عتاد: تسلسل بعد إعادة التشغيل، ورفض `expiresAt` عند `clockTrusted=false`. مشترك MQTTnet لا يغلق هذا البند.
4. تخزين ESP32 المحلي عند انقطاع الشبكة. persistence على Mosquitto لا تغلق هذا البند.
5. اختبار عميل Flutter أو Web على العقد المصدَّر: `202` لا يغيّر الحالة المعروضة كمؤكدة.
6. محضر مراجعة ESP + Backend + Flutter. بعده فقط يمكن طلب اعتماد G1.

إلى أن يحدث ذلك يبقى القرار `G1 BLOCKED`.
