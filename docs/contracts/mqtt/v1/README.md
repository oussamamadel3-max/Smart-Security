# MQTT draft v1 — مستخرج من الكود، وليس اعتماد G1

**الوصي:** Integration · **الحالة:** `DRAFT ACCEPTED` (INT-T-01 أُغلقت 2026-11-22)  
**وثيقة التوحيد:** [`../../../integration/contracts/MQTT-V1-DRAFT.md`](../../../integration/contracts/MQTT-V1-DRAFT.md)  
**فهرس الملكية:** [`../../../integration/contracts/README.md`](../../../integration/contracts/README.md)

هذه الملفات تصف الشكل الذي يبنيه أو يقبله الكود اليوم. **مسودة مقبولة ضمن INT-T-01** وليست عقداً إنتاجياً معتمداً. بوابة **G1 تبقى BLOCKED** حتى INT-T-04 والأدلة.

## المصادر

- أمثلة خطة التطوير §4.4 في `docs/خطة_تطوير_نظام_الأمان_الذكي_v1.0.md`.
- `OwnerDeviceCommandService.BuildExecuteCommandPayload`
- `DeviceInboundTelemetryService.ParseState` / `ParseHeartbeat` / `ParseCommandAck`
- `AlarmEventIngestService.ParseAlarmEvent` / `ParseEventLoss` / `ParseDeviceFault` / `BuildEventPersistedPayload`
- `DeviceOwnershipService.BuildOwnershipSyncPayload`
- ثوابت `ExecuteCommandDraft` و`MqttInboundDraft` و`AlarmEventDraft` و`EventPersistedDraft` و`OwnershipSyncDraft` و`DbStatusCodes`

`schemaVersion` في كل الملفات = `1`.

## القنوات والنقل

| Topic | الناشر | المشترك | QoS في المرجع | Retained |
|---|---|---|---|---|
| `devices/{deviceId}/state` | الجهاز | الخادم | 0 للنبضة `HEARTBEAT`. رسائل `STATE` المؤثرة لم تُثبَّت QoS لها باختبار إعادة تسليم | لا يُستخدم retained للأوامر |
| `devices/{deviceId}/events` | الجهاز | الخادم | 1 | — |
| `devices/{deviceId}/acks` | الجهاز | الخادم | 1 | — |
| `devices/{deviceId}/commands` | الخادم | الجهاز | 1 لـ `EXECUTE_COMMAND` و`OWNERSHIP_SYNC` و`EVENT_PERSISTED` | ممنوع للأوامر اللحظية |

`IMqttOutboundPublisher` و`LiveMqttOutboundPublisher` ينشران QoS 1 و`retain=false`. هذا وصف كود، لا دليل broker إلا إذا سجّله `docs/gates/G1-evidence.md`.

الهوية: الحمولة `deviceId` يجب أن تطابق قناة الموضوع وهوية الاعتماد. الخادم يرفض الاختلاف بالرمز `DEVICE_IDENTITY_MISMATCH`. ACL على Mosquitto ليست مضمّنة في هذا المجلد؛ إعداد الاختبار المعزول في `security_backEnd/API.IntegrationTests/mosquitto/`.

التكرار: `messageId` مطلوب على الرسائل الواردة. تفرد الحادثة `(device_id, event_uid)`. إغلاق الأمر يتبع `commandId`. مخطط JSON لا يفرض أحادية التسليم.

عدم التراجع: إذا كان `stateSequence` أو `resultStateSequence` أصغر من أو يساوي التسلسل المخزن، لا تُحدَّث `device_states`. `COMMAND_ACK` المتأخر قد يغلق الأمر دون تحريك الحالة (`COMMAND_CLOSED_ONLY`). النبضة تحدّث `last_seen_at` فقط.

## مرحلة الحادثة

نفس `ALARM_EVENT` ونفس `eventUid`. `alarmPhase=ENDED` عند عودة الحساس. غياب الحقل يعني `ACTIVE`. لا `messageType` جديد. الكتم لا ينهي الحادثة. `EVENT_PERSISTED` يقر مرحلة واحدة في كل نشر: `alarmPhase` على السلك، و`stored=true` بعد commit فقط. إقرار قديم بلا `alarmPhase` يقر `ACTIVE` ولا يمسح نهاية محفوظة. الحقل الداخلي `queuedPhases` لا يُنشر.

## ما لم يُنشأ له مخطط

- **`SETTINGS_SYNC`:** مؤجل خارج v1 مع OTA. لا schema. العبارة في SRS مشروطة باعتماد التهيئة عن بعد.
- **TTL والسعة والتسلسل عبر إعادة التشغيل:** تبقى `OPEN`. `ApprovedDeviceSettings.HeartbeatTimeoutSeconds = 3600` و`CommandTimeoutSeconds = 10` و`TestDurationMs = 30000` قيم اختبار ابتدائية وليست قياساً معتمداً لـ `D-03` / `D-05` / `NFR-008`. مبدأ استمرار `stateSequence` بعد إعادة التشغيل معتمد، وآلية NVS غير مجمّدة قبل اختبار انقطاع الطاقة.

## تعارضات لا يُحسمها هذا المجلد

1. `confirmedOwnershipVersion` و`clockTrusted` اختياريان في `COMMAND_ACK` v1. غيابهما لا يرفع إصدار الملكية المؤكد ولا يغيّر ثقة الساعة. الجهاز الجديد يرسلهما. الحقلان إلزاميان في `STATE`.
2. رسائل الخادم (`EXECUTE_COMMAND`, `EVENT_PERSISTED`, `OWNERSHIP_SYNC`) بلا `bootId`. رسائل الجهاز تطلب `bootId`.
3. `additionalProperties` مسموح لأن الـ parser يتجاهل الحقول الزائدة ولا يرفضها.
4. `expiresAt` الصادر من الخادم هو `yyyy-MM-dd'T'HH:mm:ss'Z'` بلا كسور ثانية. أوقات الجهاز الاختيارية تُقبل بأي نص يمر على `DateTimeOffset.TryParse`، والمخطط هنا يتحقق من النوع فقط.
5. `faultCode` نص مفتوح. القيمة `SIREN_DRIVER` في العينة توضيحية وليست فهرس أعطال معتمداً.
6. حالة ACK على السلك هي `EXECUTED` أو `REJECTED` فقط. `PENDING` و`TIMED_OUT` و`CANCELLED` حالات أمر على الخادم وليست قيم `status` في `COMMAND_ACK`.

## العينات

- `samples/accept/` يجب أن تنجح.
- `samples/reject/` يجب أن تفشل.
- `samples/ordering/` — أمثلة ترتيب ACK/STATE (INT-T-02)؛ كل ملف يطابق مخطط `state` أو `command-ack`. القواعد: [`../../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md`](../../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md).
- الاختبار: `Application.Tests/MqttDraftV1SchemaTests.cs` (قبول/رفض + `Ordering_Scenario_Messages_Match_Schemas`).
