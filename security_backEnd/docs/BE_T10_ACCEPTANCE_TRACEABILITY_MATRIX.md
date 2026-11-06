# BE-T-10 — مصفوفة تتبع سيناريوهات القبول الخلفية (SS-P9-030)

التاريخ: 2026-11-06  
المهمة: `BE-T-10` / البطاقات `SS-P9-010` · `SS-P9-020` · `SS-P9-030`  
المرجع: خطة التطوير §12 · `integration/integration-acceptance-matrix.md` (`AC-T-01`…`AC-T-25`) · SRS القسم 12  
بوابة العقود: **G1 غير معتمدة** · MQTT الحي / Mosquitto: **غير متاح في بيئة التنفيذ الحالية** · FCM إنتاجي: **معطّل (Fake فقط)**

> هذه المصفوفة تتبع **دليل الخادم** فقط. نجاح اختبارات Backend لا يغلق `AC-T-*` على staging/ميداني ولا يغني عن اختبارات قبول المهام `BE-T-02`…`BE-T-09` المستقلة.  
> التصنيفات: `VERIFIED` · `PARTIAL` · `BLOCKED` · `NOT VERIFIED`.

## مفاتيح التصنيف

| التصنيف | المعنى |
|---------|--------|
| `VERIFIED` | نُفّذ اختبار خادمي يثبت الجزء الخلفي من السيناريو على PostgreSQL (أو وحدة صرفة) مع دليل أمر/ملف |
| `PARTIAL` | جزء خادمي مثبت؛ ينقص عتاد/واجهة/MQTT حي/G1/Firebase |
| `BLOCKED` | متعذر الآن بسبب اعتماد G1 أو غياب Broker/عتاد/Ops |
| `NOT VERIFIED` | لا دليل خادمي كافٍ ضمن نطاق BE-T-10 |

## مسارات حرجة مطلوبة في التفويض

| المسار | دليل أساسي | النتيجة |
|--------|------------|---------|
| `POST Command → Outbox → MQTT → COMMAND_ACK → حالة + سجل أمر` | `BackendAcceptancePathTests.Post_Command_Outbox_FakePublish_Ack_...` + `DeviceCommandLifecycleTests`؛ MQTT الحي عبر `LiveMqttBrokerIntegrationTests` | `PARTIAL` — PostgreSQL+Fake publish `VERIFIED`؛ Broker حي `BLOCKED`/`NOT VERIFIED` (Skip) |
| `ALARM_EVENT → حفظ → طلب إشعار → EVENT_PERSISTED` | `AlarmEventIngestTests` + `BackendAcceptancePathTests.Alarm_Event_Persists_...` | `PARTIAL` — DB/Outbox `VERIFIED`؛ نشر MQTT حي لـ EVENT_PERSISTED `BLOCKED` |
| نقل / تعارض / مهلة / إعادة تسليم / حساب معطّل | `OwnershipTransferHttpTests` · `OwnerDeviceCommandHttpTests` · `DeviceCommandLifecycleTests` · `AuthHttpTests` / `BackendAcceptancePathTests.Disabled_Owner_...` + وحدات SS-P9-010 | `PARTIAL` إلى `VERIFIED` حسب الصف |

## مصفوفة AC-T مقابل اختبارات Backend

| AC-T | السيناريو | نطاق BE | اختبارات / رموز مرتبطة | التصنيف | ملاحظات |
|------|-----------|---------|------------------------|---------|---------|
| AC-T-01 | إنذار محلي بلا إنترنت | ESP | — | `NOT VERIFIED` | خارج الخادم |
| AC-T-02 | لا إنذار عند DISARM | ESP | — | `NOT VERIFIED` | خارج الخادم |
| AC-T-03 | ARM من الويب + تأخر ACK | BE+UI | `OwnerDeviceCommandHttpTests.Post_Arm_Returns_202_Pending_...` · `DeviceCommandLifecycleTests.CommandAck_...` · `TimedOut_Blocks_...` · وحدة `HttpAcceptanceChangesConfirmedArmState` | `PARTIAL` | HTTP 202 لا يغيّر Arm؛ ACK يحدّث الحالة. واجهة الويب وMQTT الحي وG1 ناقصة |
| AC-T-04 | إعادة أمر/حدث مرة واحدة | BE+ESP | `OwnerDeviceCommandHttpTests.Idempotent_Replay_...` · `AlarmEventIngestTests.Duplicate_And_Concurrent_...` · `BackendAcceptancePathTests.Alarm_Event_...` · وحدات idempotency/duplicate | `PARTIAL` | تكرار الخادم مثبت على PostgreSQL؛ تنفيذ العتاد مرة واحدة يحتاج ESP |
| AC-T-05 | أمران متعارضان | BE+Mobile | `OwnerDeviceCommandHttpTests.Concurrent_Conflicting_Commands_Only_One_Pending` · وحدة `BlocksConflictingCommand` | `PARTIAL` | تعارض الخادم مثبت؛ هاتفان حقيقيان خارج النطاق |
| AC-T-06 | MUTE أثناء إنذار | BE+ESP+UI | `OwnerDeviceCommandHttpTests.Mute_Requires_Matching_Current_Alarm_Uid` · وحدة Mute | `PARTIAL` | بوابة الخادم مثبتة؛ صوت الجهاز/ESP ناقص |
| AC-T-07 | اختبار صفارة ثم إنذار | ESP | `OwnerDeviceCommandHttpTests.TestSiren_Rejected_During_Alarm_...` | `PARTIAL` | رفض الخادم أثناء إنذار؛ أولوية العتاد ESP |
| AC-T-08 | تعطل DB أثناء حدث | ESP+BE | `AlarmEventIngestTests.Rejected_Or_Failed_Ingest_...` · سباقات Outbox | `PARTIAL` | فشل الالتزام لا يُعد حفظاً؛ تعطل DB حقيقي/PUBACK على ESP غير مختبر هنا |
| AC-T-09 | امتلاء سعة الأحداث | ESP | — (D-03 معلّق) | `BLOCKED` | ينتظر `D-03` / ESP |
| AC-T-10 | نقل جهاز غير متصل | BE+ESP | `OwnershipTransferHttpTests.New_Owner_Offline_Is_Rejected_...` · `Prior_Owner_Loses_Access_...` · وحدات publish-hold | `PARTIAL` | عزل API مثبت؛ OWNERSHIP_SYNC عبر Broker حي `BLOCKED` |
| AC-T-11 | جهاز/حساب أجنبي + قناة أجنبية | BE+ESP | `OwnerDeviceCommandHttpTests.Foreign_Owner_Cannot_Create_Command` · اختبارات ملكية/وارد | `PARTIAL` | رفض API مثبت؛ ACL Mosquitto للجهاز الأجنبي `NOT VERIFIED` |
| AC-T-12 | فشل FCM / إذن معطّل | BE+Mobile | `FcmNotificationDispatchTests.Temporary_Failure_...` · Fake FCM | `PARTIAL` | Fake فقط؛ Firebase الحي معطّل عمداً |
| AC-T-13 | انقطاع طاقة جهاز | ESP | — | `NOT VERIFIED` | خارج الخادم |
| AC-T-14 | بلا تشخيص → UNKNOWN | ESP+UI | عرض Owner/Admin لا يساوي «سليم» بلا دليل (T-04/T-08) | `PARTIAL` | منطق عرض خادمي؛ تحقق UI/عتاد جزئي |
| AC-T-15 | إعادة تشغيل VPS / نسخة | Ops | — | `NOT VERIFIED` | `ops` |
| AC-T-16 | PENDING ثم إقرار | BE+ESP+UI | `DeviceOwnershipHttpTests` (ربط/إقرار) | `PARTIAL` | PENDING/ACTIVE على الخادم مثبت؛ MQTT OWNERSHIP_SYNC حي ناقص |
| AC-T-17 | TIMED_OUT ثم متعارض | BE | `DeviceCommandLifecycleTests.TimedOut_Blocks_Conflicting_Until_State_Reconciliation` · وحدات timeout/block | `VERIFIED` (خادم/PostgreSQL) | قيم المهلة النهائية مرتبطة لاحقاً بـ `D-03`/`NFR-008` |
| AC-T-18 | MUTE بمعرف حادثة تغيّر | ESP | بوابة `MuteTargetMismatch` على الخادم | `PARTIAL` | رفض الخادم عند عدم المطابقة؛ قرار الجهاز النهائي ESP |
| AC-T-19 | أرشفة + إبطال Broker | BE+Ops | `DeviceArchiveHttpTests` + `NoOpDeviceBrokerAccessService` | `PARTIAL` / `BLOCKED` للـACL الحي | No-Op تطويري؛ Mosquitto ACL غير مثبت |
| AC-T-20 | حساب معطّل برمز قديم | BE | `AuthHttpTests.Disabled_User_Access_Is_Rejected_Immediately` · `BackendAcceptancePathTests.Disabled_Owner_Cannot_Post_Command_...` · `AccountAccessRulesTests` | `VERIFIED` (خادم) | رفض `/me` و`POST` أمر |
| AC-T-21 | FK مركّب جهاز/حساس/حادثة | BE | `DatabaseConstraintTests` | `VERIFIED` | PostgreSQL |
| AC-T-22 | كتم مع بقاء الإشارة | ESP | — | `NOT VERIFIED` | سلوك صفارة عتاد |
| AC-T-23 | wasBuffered + ساعة غير موثوقة | BE+UI | `AlarmEventIngestTests.WasBuffered_And_Untrusted_Clock_...` · وحدات clock | `PARTIAL` | حفظ الخادم مثبت؛ عرض UI ناقص |
| AC-T-24 | نقل + إشعار معلّق + ADMIN_ALERT | BE | `OwnershipTransferHttpTests.Transfer_Cancels_Prior_Alarm_...` · `FcmNotificationDispatchTests.Ownership_Transfer_During_Queue_...` | `VERIFIED` (خادم/Fake FCM) | Firebase الحي خارج الإثبات |
| AC-T-25 | تسجيل بلا settings معتمدة | BE | اختبارات تسجيل الجهاز (T-03) | `VERIFIED` / راجع `DeviceRegistrationHttpTests` | لا جهاز جاهز بلا إعدادات معتمدة |

## بطاقات BE-T-10

| بطاقة | الحالة التنفيذية | دليل |
|-------|------------------|------|
| SS-P9-010 | منفَّذة اختباريًا (وحدات قواعد) | تشغيل 2026-11-06: `Application.Tests` **80/80 Passed** (`OwnerDeviceCommandAcceptanceRulesTests` · `AccountAccessRulesTests` · `OwnershipTransferAcceptanceRulesTests` · `AlarmEventAcceptanceRulesTests` + وحدات سابقة) |
| SS-P9-020 | منفَّذة جزئيًا | تشغيل 2026-11-06: جديد `BackendAcceptancePathTests`+probe **4 Passed**؛ `Outbox_Dispatch_Delivers_...` **Skipped** (لا Mosquitto/Docker)؛ انحدار أوامر/حوادث/ملكية/FCM/Auth **71/71 Passed** على PostgreSQL |
| SS-P9-030 | منفَّذة | هذا الملف |


## متطلبات الإغلاق النهائي بعد G1 وتجهيز MQTT

1. اعتماد `INT-T-04` / G1 لعقود `EXECUTE_COMMAND` / `COMMAND_ACK` / `ALARM_EVENT` / `EVENT_PERSISTED` / `OWNERSHIP_SYNC`.
2. تشغيل Mosquitto معزول (Compose/`ops` أو Testcontainers) ونجاح `Outbox_Dispatch_Delivers_ExecuteCommand_To_Real_Broker_Subscriber` بدون Skip.
3. محوّل اشتراك وارد حي لـ COMMAND_ACK/STATE/ALARM_EVENT (ليس Ingest المباشر من الاختبار فقط).
4. عدم اعتبار HTTP 202 أو MQTT PUBACK تنفيذاً؛ إثبات الحالة الفعلية من ACK/STATE في PostgreSQL.
5. إبقاء Firebase/`Fcm:Enabled` معطّلين حتى تفويض منفصل؛ Fake لا يغني عن قبول FCM الحي لـ AC-T-12 عند الحاجة.
6. موافقة صريحة: `APPROVE TASK CLOSURE: BE-T-10` بعد استيفاء الأدلة — دون تعليم `[x]` أو تحديث نسب نهائية قبلها.
7. نجاح BE-T-10 لا يغلق رسمياً BE-T-05…BE-T-08 ولا يستبدل مصفوفة G2 staging/ميداني.
