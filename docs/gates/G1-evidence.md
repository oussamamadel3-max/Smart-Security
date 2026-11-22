# دليل بوابة G1

الحكم الحالي: **G1 BLOCKED**. آخر تشغيل موثّق: 2026-11-17 (تحقق Backend النهائي). هذا الملف يسجل ما شُغّل فعلاً. لا يعني اعتماد البوابة ولا إغلاق `INT-T-01`…`INT-T-04` أو أي `BE-T-*`.

إضافة TLS والموثوقية في أسفل سجل 2026-11-16. الإنتاج يبقى `UseTls=false` و`LivePublishEnabled=false` و`Mqtt:Inbound:Enabled=false` و`Mqtt:Outbox:Enabled=false`.

سجل 2026-11-15 يبقى أدناه كدليل PostgreSQL والمخططات. نتائج الوسيط في ذلك اليوم أُعيد فحصها في 2026-11-16؛ البنود في «معيار G1» تعكس آخر نتيجة.

## إضافة 2026-11-22 — OPS-T-03 تحقق محلي تكميلي (ليس إغلاق G1 ولا Staging عام)

- Forwarded Headers: ثقة صريحة فقط (`ForwardedHeadersSetup`). المختبر يفعّل شبكات Docker الخاصة ويبقي API بلا منفذ مضيف. Caddy يحذف ترويسات العميل.
- `Test-StagingLocalRuntime.ps1`: بناء `smartsecurity-api:local-stg`، `/health/live` و`/health/ready` عبر HTTPS محلي (Caddy `local_certs`)، إعادة تشغيل، بقاء `smartsecurity_stg_pgdata` بعد `down` بلا `-v`. **PASS**.
- هذا الدليل = **Local Runtime Validated**. H1–H7 على مضيف عام تبقى **NOT VERIFIED**.
- **G1 يبقى BLOCKED.** MQTT إنتاجي DISABLED. OPS-T-03 **لم تُغلق**.
- التقرير التكميلي: `docs/reports/ops/OPS-T-03-supplement-report.md`.

## إضافة 2026-11-22 — OPS-T-03 قوالب Staging + probes (ليس إغلاق G1)

- قوالب: `ops/staging/` (compose 127.0.0.1، Caddy، Dockerfile.api، ROLLBACK، HTTPS-ACCEPTANCE H1–H7)
- Backend: `GET /health/live` · `GET /health/ready` (DbContext) · Forwarded Headers
- تحقق:

```text
cd ops
.\scripts\Test-OpsT03.ps1
# PASS — config/network/https-checklist/secrets/dev health

dotnet test API.IntegrationTests --filter FullyQualifiedName~HealthProbeTests
# Passed 3
```

- HTTPS حي على مضيف staging عام: **NOT VERIFIED** (DEP-OPS-03 / DEP-WEB-01).
- **G1 يبقى BLOCKED.** MQTT إنتاجي DISABLED. OPS-T-03 **لم تُغلق** بعد.
- التقرير: `docs/reports/ops/OPS-T-03-report.md`.

## إضافة 2026-11-22 — OPS-T-02 عزل بيئات/أسرار (ليس إغلاق G1)

- وثائق/قوالب: `ops/environments/` · `ops/docs/ENV-ISOLATION.md` · `SECRETS-AND-JWT.md` · `WEB-SECURITY-PER-ENV.md`
- تحقق محلي:

```text
cd ops
.\scripts\Test-OpsT02.ps1
```

- نتيجة الجلسة: Secrets hygiene PASS · Env templates PASS · Compose healthy · ACL **9/9 PASS**.
- HTTPS حي على staging: **NOT RUN** (لا مضيف — مجهَّز لـ OPS-T-03).
- **G1 يبقى BLOCKED.** MQTT إنتاجي DISABLED.
- **OPS-T-02 أُغلقت رسمياً** 2026-11-22 (`OPS-T-02-CLOSURE-REPORT.md`) — عزل بيئات/أسرار فقط؛ HTTPS الحي مؤجّل لـ OPS-T-03؛ لا يغيّر حكم G1 أعلاه.

## إضافة 2026-11-22 — INT-T-03 مسودة HTTP / OpenAPI (ليس إغلاق G1)

- وثائق: `integration/contracts/HTTP-V1-DRAFT.md` · `HTTP-ENDPOINT-INVENTORY.md` · `INT-T-03-CROSS-PROJECT-REVIEW.md`
- OpenAPI: `docs/contracts/http/openapi.json` (منفَّذ + overlays) · `openapi.proposed.json` · `error-codes.md` · `samples/`
- اختبارات (من `security_backEnd/` وFlutter):

```text
dotnet test Application.Tests/Application.Tests.csproj --filter "FullyQualifiedName~HttpOpenApiDraftTests|FullyQualifiedName~OwnerDeviceCommandAcceptanceRulesTests|FullyQualifiedName~OwnerDeviceCommandRulesTests"
# Failed 0, Passed 33, Skipped 0

dotnet test API.IntegrationTests/API.IntegrationTests.csproj --filter "FullyQualifiedName~OwnerDeviceCommandHttpTests|FullyQualifiedName~OpenApiDraftExportTests"
# Failed 0, Passed 9, Skipped 0  (PostgreSQL عبر OPS-T-01 / ESP32_SECURITY_TEST_DB)

flutter test test/features/devices/device_command_entity_test.dart test/features/devices/command_poll_policy_test.dart
# (security_mobile) All tests passed — 5

flutter test test/features/devices/device_detail_command_guards_test.dart
# (admin_web) All tests passed — 6
```

- **G1 يبقى BLOCKED.** لا تفعيل MQTT إنتاجي.
- **INT-T-03 أُغلقت رسمياً** 2026-11-22 (`INT-T-03-CLOSURE-REPORT.md`) — قبول مسودة HTTP/OpenAPI فقط؛ لا يغيّر حكم G1 أعلاه. مخططات استجابة OpenAPI **ليست** مكتملة لجميع DTOs (فجوة موثّقة PROP-HTTP-03).

## إضافة 2026-11-22 — INT-T-02 توثيق D-09 / ترتيب ACK·STATE (ليس إغلاق G1)

- وثيقة: `integration/contracts/D-09-COMMAND-ACK-AND-STATE.md`
- عينات ترتيب: `docs/contracts/mqtt/v1/samples/ordering/` (12 JSON)
- مراجعة توافق: `integration/contracts/INT-T-02-CROSS-PROJECT-REVIEW.md`
- اختبارات مخطط (من `security_backEnd/`):

```text
dotnet test Application.Tests/Application.Tests.csproj --nologo --filter FullyQualifiedName~MqttDraftV1SchemaTests
```

- النتيجة: **Failed 0, Passed 45, Skipped 0** (exit 0) — يشمل عينات القبول/الرفض + ترتيب INT-T-02.
- انحدار وارد ACK/STATE على PostgreSQL:

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter "FullyQualifiedName~DeviceInboundTelemetryTests|FullyQualifiedName~DeviceCommandLifecycleTests.Late_Ack"
```

- النتيجة: **Failed 0, Passed 10, Skipped 0** (exit 0).
- **G1 يبقى BLOCKED.** لا تفعيل MQTT إنتاجي.
- **INT-T-02 أُغلقت رسمياً** 2026-11-22 (`INT-T-02-CLOSURE-REPORT.md`) — قبول توثيق D-09 فقط؛ لا يغيّر حكم G1 أعلاه.

## إضافة 2026-11-22 — OPS-T-01 مكدس محلي (ليس إغلاق G1)

- مسار: `ops/docker-compose.yml` — `postgres:16-alpine` + `eclipse-mosquitto:2` (broker **2.1.2**).
- منافذ مضيف معزولة: `127.0.0.1:5433` (Postgres)، `127.0.0.1:1883` / `8883` (MQTT نصي/TLS).
- ACL محلي بهويات `srv`/`deva`/`devb` وفق مسودة MQTT v1؛ مصفوفة OPS: **9/9 PASS** عبر عميل paho MQTT v5 (PUBACK 135 للرفض؛ 0/16 للسماح).
- TLS اختبار محلي (طبقة أ/ب): CA موثوق ينجح؛ بدون CA / CA غير موثوق يُرفض. **لا** نسخ إلى إنتاج؛ `appsettings.json` يبقى `UseTls=false` وLivePublish/Inbound/Outbox معطّلة.
- الحكم على البوابة: **G1 يبقى BLOCKED**. نجاح OPS-T-01 ≠ G1-03 مغلق ≠ PRODUCTION READY.
- **OPS-T-01 أُغلقت رسمياً** 2026-11-22 (`OPS-T-01-CLOSURE-REPORT.md`) — الإغلاق تشغيلي محلي فقط؛ لا يغيّر حكم G1 أعلاه.

## إضافة 2026-11-21 — قرارات مالك بلا أدلة تشغيل جديدة

تثبيت قرارات المالك في [`../reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md`](../reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md) **لم يُشغّل** اختبارات جديدة ولم يغيّر نتائج G1-01…G1-11 أدناه. قرارات العقد المعتمدة ≠ اجتياز البوابة. كتالوج الاعتماديات: [`G0-G1-remaining-dependencies.md`](./G0-G1-remaining-dependencies.md).

## إضافة 2026-11-21 — INT-T-01 (مسودة Integration)

- ملكية المسودة: `integration/contracts/MQTT-V1-DRAFT.md` (DRAFT/UNDER REVIEW).
- إعادة تشغيل تحقق المخططات من `security_backEnd/`:

```text
dotnet test Application.Tests/Application.Tests.csproj --nologo --filter FullyQualifiedName~MqttDraftV1SchemaTests
```

- النتيجة الفعلية (تنفيذ INT-T-01): **Failed 0, Passed 32, Skipped 0, Total 32, Duration 2 s** (exit 0).
- إعادة تشغيل 2026-11-22 (مراجعة ما قبل الإغلاق): **Failed 0, Passed 32, Skipped 0, Total 32, Duration ~911 ms** (exit 0). يثبت توافق العينات مع مخططات المسودة فقط — **ليس** اعتماد G1 ولا إغلاق INT-T-01. التفاصيل: `docs/reports/integration/INT-T-01-CLOSURE-REVIEW.md`.

## إضافة 2026-11-16 — مسار Backend وMosquitto

الحكم لم يتغير: **G1 BLOCKED**. لم يُغيَّر أي عقد MQTT معتمد. هذا القسم نصي بلا TLS. تشغيل TLS لاحقاً في «TLS والموثوقية». لا جهاز ESP32.

### ثلاثة مستويات منفصلة

| المستوى | النتيجة |
|---|---|
| Broker حقيقي مع عميل MQTT تجريبي | PASS جزئي للنص الصريح فقط. `IsolatedMosquittoAclTests`: Failed 0, Passed 7, Skipped 0, Total 7, Duration 41.2467 s. الصورة `eclipse-mosquitto:2`، الإصدار `2.1.2`، المعرّف `sha256:75d6662dee52c947dc9dd9c29f1d8d7ffef78ddaf38001caa09755681e8bbc98`. |
| تكامل Backend حقيقي مع Broker | صادر ووارد على PostgreSQL. التفاصيل في «المستهلك الفعلي» أدناه. الإنتاج يبقى `NoOpMqttOutboundPublisher` لأن `Mqtt:Outbound:LivePublishEnabled` في `appsettings.json` يساوي `false`. |
| تكامل ESP32 فعلي | NOT RUN. لا firmware في `security_esp/`. مشترك MQTT تجريبي ليس إثبات firmware. |

### 1) `EXECUTE_COMMAND` من Backend إلى مشترك تجريبي

الأمر من `security_backEnd/`:

```text
$env:ESP32_SECURITY_TEST_MQTT_HOST='127.0.0.1'
$env:ESP32_SECURITY_TEST_MQTT_PORT='1883'
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter FullyQualifiedName~LiveMqttBrokerIntegrationTests
```

الوسيط: حاوية `ss-g1-anon`، `allow_anonymous true`، مستمع `1883` على `127.0.0.1` فقط، بلا ACL وبلا TLS. `TCP 127.0.0.1:1883` نجح و`TCP 127.0.0.1:8883` فشل في فحص لاحق.

`Outbox_Dispatch_Delivers_ExecuteCommand_To_Real_Broker_Subscriber` استدعى `IOutboxDispatchService.DispatchPendingCommandsAsync` ثم استلم مشترك MQTTnet تجريبي الرسالة. التحققات: `messageType=EXECUTE_COMMAND`، نفس `commandId`، `QualityOfServiceLevel=AtLeastOnce`، `Retain=false`، حالة الأمر في PostgreSQL بقيت `PENDING`، و`armState` بقي `DISARMED`. HTTP كان `202`. هذا تسليم وسيط، لا تنفيذ جهاز.

تسجيل الإنتاج ما زال `NoOpMqttOutboundPublisher` و`IsLivePublishEnabled=false` في `InfrastructureServiceRegistration`. الاختبار يستبدل الناشر داخل مضيف الاختبار فقط.

### 2) `STATE` و`COMMAND_ACK` عبر المستهلك الفعلي

كان NOT RUN في الفحص الأول من هذا اليوم، حين كان `MqttInboundHostedService` لا يستدعي `Subscribe`. بعد التنفيذ، القسم «المستهلك الفعلي» يثبت الوصول إلى PostgreSQL عبر هذا المستهلك عند تفعيل الإعداد. الإنتاج يبقى غير مفعّل في `appsettings.json`.

### 3) QoS 1 وعدم retained للأوامر اللحظية

مثبت على السلك في اختبارين منفصلين، كلاهما عميل تجريبي أو مسار الصادر أعلاه:

- Backend → مشترك تجريبي: QoS `AtLeastOnce` و`Retain=false` على `EXECUTE_COMMAND`.
- عميلان تجريبيان على broker بكلمات مرور: `Server_Command_Reaches_DeviceA_At_Qos1_Without_Retain` و`Unretained_Command_Is_Not_Delivered_To_A_Later_Subscriber`. المشترك المتأخر لم يستلم الأمر غير المحتفظ. `Heartbeat_Publish_Uses_Qos0` أثبت QoS 0 لنبضة على قناة `state`، وهو خارج أوامر اللحظة.

### 4) عزل ACL

الأمر:

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter FullyQualifiedName~IsolatedMosquittoAclTests --logger "console;verbosity=detailed"
```

النتيجة: Failed 0, Passed 7, Skipped 0, Total 7. الاختبار يشغّل حاويته بكلمات مرور مولَّدة في الذاكرة، وغير مكتوبة هنا. المستخدمون `srv` و`deva` و`devb`.

- `Anonymous_Connect_Is_Rejected`
- `DeviceA_Cannot_Publish_On_DeviceB_State`: النشر على `devices/{deviceB}/state` أعاد `NotAuthorized`
- `DeviceA_Does_Not_Receive_DeviceB_Commands`: اشتراك `deva` على أوامر الجهاز B لم يسلّم رسالة نشرها `srv` خلال 3 ثوانٍ
- `Server_Command_Reaches_DeviceA_At_Qos1_Without_Retain`
- `Unretained_Command_Is_Not_Delivered_To_A_Later_Subscriber`
- `Heartbeat_Publish_Uses_Qos0`
- `Docker_Probe_Does_Not_Claim_Acl_Success_By_Itself` يسجّل البيئة ولا يُحتسب وحده نجاح ACL

ملاحظة Mosquitto `2.1.2`: رفض العزل أُثبت بعدم التسليم وبرمز نشر `NotAuthorized`. لم يُشترط أن يرفض `SUBACK` الاشتراك؛ منح الاشتراك ثم عدم التسليم هو السلوك الذي نجح الاختبار عليه. هذا ليس مسار Backend ولا ESP32. TLS غير مشغّل، فنجاح ACL النصي لا يجتاز G1-03.

### 5) التكرار وSTATE القديمة

لم يُعد تشغيلها عبر الوسيط. تبقى من تشغيل 2026-11-15 على PostgreSQL بالالتقاط المباشر: `Older_Sequence_Does_Not_Overwrite...` و`Concurrent_State_Updates_Keep_Highest_Sequence_On_PostgreSQL`. لا تُرفع إلى إثبات إعادة تسليم MQTT ولا إلى مرور المستهلك الفعلي. G1-06 يبقى BLOCKED.

### 6) عدم استخدام الناشر المزيّف في إثبات الاتصال

`LiveMqttBrokerIntegrationTests` يحقن `LiveMqttOutboundPublisher` فقط. `RecordingMqttOutboundPublisher` يبقى في اختبارات PostgreSQL الأخرى (62 ناجحة / 1 كان متخطى في 2026-11-15) ولا يُستخدم دليلاً على اتصال حقيقي.

### TLS وESP32

- TLS في هذا الموضع من السجل: كان NOT RUN. الدليل اللاحق في «TLS والموثوقية». عبارة «TLS support available» وحدها ليست اتصالاً.
- ESP32: NOT RUN. المشترك التجريبي عميل MQTTnet داخل الاختبار.

## المستهلك الفعلي — لاحقاً في 2026-11-16

`MqttInboundHostedService` صار يشترك في `devices/+/state` و`devices/+/acks` و`devices/+/events` عند `Mqtt:Inbound:Enabled=true`، ويعيد الاتصال والاشتراك بعد انقطاع الوسيط. كل رسالة تذهب إلى `MqttInboundDispatcher` ثم `DeviceInboundTelemetryService` أو `AlarmEventIngestService`. هوية الجهاز تُؤخذ من القناة، ثم تُرفض الحمولة إذا اختلف `deviceId`. لا يُنسخ `deviceId` من JSON وحده.

`MqttNetLivePublisher` يُسجَّل فقط عندما `Mqtt:Outbound:LivePublishEnabled=true`. وإلا يبقى `NoOpMqttOutboundPublisher`. الملف `API/appsettings.json` يبقي الاثنين `false`، وكذلك `Mqtt:Outbox:Enabled`. اختبار `Committed_AppSettings_Keep_Live_Mqtt_Disabled` يثبت ذلك. التفعيل في الاختبار يتم بإعدادات مضيف الاختبار، لا بتغيير الإنتاج. لا كلمة مرور في Git.

الأمر من `security_backEnd/`:

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter FullyQualifiedName~LiveMqttInboundIntegrationTests
```

النتيجة: Failed 0, Passed 7, Skipped 0, Total 7, Duration 3 m 21 s. الوسيط حاوية `eclipse-mosquitto:2` مجهولة على `127.0.0.1` ومنفذ ثابت، بلا TLS وبلا persistence. الناشر هو `MqttNetLivePublisher`. الاختبارات لا تستدعي خدمات الالتقاط مباشرة ولا تستخدم `RecordingMqttOutboundPublisher`.

- `Http_Command_Reaches_Subscriber_At_Qos1_Without_Retain`: HTTP `202` ثم Outbox ثم Mosquitto ثم مشترك تجريبي. QoS 1 و`Retain=false`. الأمر بقي `PENDING` و`armState` بقي `DISARMED`.
- `State_Heartbeat_And_Payload_Mismatch_Go_Through_The_Hosted_Consumer`: `STATE` و`HEARTBEAT` وصلا إلى `device_states`. حمولة `deviceId` المخالفة للقناة لم تغيّر الجهاز الآخر.
- `Ack_Order_Duplicate_And_Stale_State_Do_Not_Rewind_Confirmed_State`: `COMMAND_ACK` قبل `STATE` وبعدها. تكرار نفس `stateSequence` وتسلسل أقدم لم يرجّعا `armState`. الصف النهائي في PostgreSQL بقي على أعلى تسلسل.
- `Late_Ack_After_Timeout_Closes_Command_Without_Rewinding_State`: `MarkTimedOutCommandsAsync` ثم ACK متأخر عبر المستهلك. الأمر أصبح `EXECUTED` مع `IsLateAck`، والتسلسل الأحدث بقي.
- `Alarm_Loss_And_Fault_Are_Stored_And_EventPersisted_Is_Published`: صف واحد لكل من `ALARM_EVENT` و`EVENT_LOSS_REPORT` و`DEVICE_FAULT` رغم تكرار الحادث. `EVENT_PERSISTED` نُشر بعد الحفظ و`stored=true`.
- `Consumer_Resubscribes_After_Broker_Restart`: بعد `docker restart` على نفس المنفذ، `STATE` التالية حُفظت. الوسيط بلا persistence، فالرسائل المنشورة أثناء الانقطاع ليست مُستعادة.

`IsolatedMosquittoAclTests` أُعيد: Failed 0, Passed 7, Skipped 0, Total 7, Duration 1 m 35 s. `MqttDraftV1SchemaTests`: Failed 0, Passed 29, Skipped 0, Total 29, Duration 1 s.

فلتر `OpenApiDraftExportTests` مع `DeviceInboundTelemetryTests` و`LiveMqttBrokerIntegrationTests`: Failed 0, Passed 10, Skipped 1, Total 11, Duration 1 m 12 s. التخطي هو نشر الاختبار القديم على `127.0.0.1:1883` لأن تلك الحاوية لم تكن تعمل؛ الإثبات الحي للمنفذ العشوائي هو `LiveMqttInboundIntegrationTests`. الالتقاط المباشر في `DeviceInboundTelemetryTests` ليس بديلاً عن المستهلك.

هذا ليس ESP32. TLS لم يُشغَّل في القسم النصي أعلاه.

## TLS والموثوقية — 2026-11-16

الحكم لم يتغير: **G1 BLOCKED**. لم يُعتمد G1. لم يُغلق أي مهمة. `API/appsettings.json` يبقى `Mqtt:Inbound:Enabled=false` و`UseTls=false` و`Mqtt:Outbound:LivePublishEnabled=false` و`UseTls=false` و`Mqtt:Outbox:Enabled=false`. الشهادات والمفتاح الخاص وكلمة مرور الاختبار بقيت في `%TEMP%` وذاكرة العملية، وليست في Git. `API.IntegrationTests/mosquitto/mosquitto.conf` الملتزم يبقى مستمعاً نصياً على `1883` مع `persistence false`. وسيط TLS يُولَّد في مجلد مؤقت: مستمع `8883` فقط، `require_certificate false`، `allow_anonymous false`، ملف ACL نفسه، `persistence true`، `persistence_location /tmp/persist/`، `autosave_interval 1`. الصورة `eclipse-mosquitto:2`. لم يُعاد استعلام digest في هذا التشغيل. البروتوكول MQTT 5. المستهلك: `CleanStart false` و`SessionExpirySeconds=3600`، ويُعاد إرسال انتهاء الجلسة عند القطع المنظم. التحقق في `MqttServerCertificateCheck.IsTrusted`: جذر مخصص، بلا CRL لأن CA الاختبار بلا قائمة إبطال، ثم صلاحية الزمن وSAN الذي يحتوي `127.0.0.1`. أي استثناء يُرجع `false`. لا يوجد معالج يرجع `true` دائماً.

الأمر من `security_backEnd/`:

```text
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
$env:NUGET_CERT_REVOCATION_MODE='offline'
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter FullyQualifiedName~LiveMqttTlsReliabilityTests --logger "console;verbosity=detailed"
```

النتيجة: Failed 0, Passed 8, Skipped 0, Total 8, Duration 4.8723 Minutes. خروج 0.

- `Committed_AppSettings_Keep_Tls_And_Live_Publish_Disabled` [33 ms]: الإنتاج يبقى بلا TLS وبلا نشر حي.
- `Default_Os_Trust_Rejects_The_Test_Certificate` [201 ms]: اتصال `UseTls` بلا الجذر المخصص رُفض. لم يُعطَّل التحقق.
- `Untrusted_Ca_Is_Rejected_And_Trusted_Ca_Receives_State` [11 s]: CA أخرى رُفضت، ثم CA الاختبار خزّنت `STATE` بالتسلسل 2.
- `Acl_Still_Isolates_Devices_Over_Tls` [6 s]: نشر `deva` على حالة الجهاز B أعاد `NotAuthorized`، ولم يستلم `deva` أمر الجهاز B خلال 3 ثوانٍ.
- `Qos1_State_Survives_Consumer_Disconnect_And_Broker_Restart` [18 s]: بعد إغلاق المستهلك نُشر `STATE` بالتسلسل 5، ثم `docker restart`. المضيف الثاني بنفس `ClientId` ومن دون مسح القاعدة حفظ `ARMED`. persistence هنا أعادت رسالة QoS 1. تشغيل النص الصريح السابق بـ `persistence false` لم يكن يستعيد ما نُشر أثناء الانقطاع.
- `Failed_Dispatch_Is_Retried_Once_After_Broker_Restart` [36 s]: `FailOnceDispatcher` رمى `IOException` مرة قبل الموزع الحقيقي، فحُجز PUBACK. بعد إعادة التشغيل حُفظ التسلسل 3 و`Successes==1`. هذا ليس إيقاف عملية PostgreSQL.
- `Outbox_Worker_Retries_When_Broker_Returns_And_Leaves_Command_Pending` [2 m 26 s]: بعد توقف الوسيط سُجّلت محاولة `AttemptCount>0` و`ProcessedAt` فارغ. بعد العودة وصل الأمر QoS 1 و`Retain=false`. الحالة بقيت `PENDING` و`armState` بقي `DISARMED`. صف الإعداد في قاعدة الاختبار فقط رُفع `CommandTimeoutSeconds` إلى 300 حتى لا يتحول الأمر إلى `TIMED_OUT` خلال الانقطاع. تشغيل سابق لنفس الاختبار، قبل هذا التعديل داخل الاختبار، وصل إلى التوكيد والحالة `TIMED_OUT` لا `EXECUTED`. ثابت الإنتاج `CommandTimeoutSeconds=10` لم يتغير.
- `Expired_Command_Is_Not_Published_Or_Executed` [16 s]: `CreatedAt` أُرجع 30 ثانية و`ExpiresAt` خمس ثوانٍ في الماضي حتى يبقى القيد `expires_at > created_at`. لم تُنشر الرسالة. `LastErrorCode=COMMAND_EXPIRED` و`ProcessedAt` مُعيَّن والحالة ليست `EXECUTED`.

PUBACK لـ QoS 1 يُرسل بعد نجاح `DispatchAsync` أو لقناة غير متوقعة. استثناء الحفظ أو التوزيع لا يرسل PUBACK، فيعيد الوسيط التسليم. فشل النشر داخل `ProcessOneAsync` يُلتقط وتُلتزَم المحاولة: يزيد `AttemptCount`، ويُضبط خطأ النشر، ويبقى `ProcessedAt` فارغاً، ولا يصبح الأمر `EXECUTED`. إذا نجح النشر ثم فشل `Commit` بعدها، فالوسيط استلم الرسالة والصف يُعاد معالجته. هذا المسار لم يُشغَّل بقتل PostgreSQL. مهلة النشر الحي 8 ثوانٍ، ثم يُقطع العميل ليعيد الاتصال. persistence على الوسيط ليست تخزين ESP32 المحلي.

### رجوع الاختبارات السابقة في التشغيل نفسه

- `LiveMqttInboundIntegrationTests`: Failed 0, Passed 7, Skipped 0, Total 7, Duration 17 m 17 s. المدة تشمل بطء Docker. النتيجة الوظيفية لم تتغير عن 7/7.
- `IsolatedMosquittoAclTests` المحاولة الأولى بعد TLS: Failed 1, Passed 6, Total 7, Duration 10 m 34 s. الفشل `DeviceA_Does_Not_Receive_DeviceB_Commands` خلال 116 ms برسالة إغلاق أثناء المصادقة. أُعيد التشغيل: Failed 0, Passed 7, Skipped 0, Total 7, Duration 14 m 10 s.
- `MqttDraftV1SchemaTests`: Failed 0, Passed 29, Skipped 0, Total 29, Duration 3 s. خروج 0.
- `OpenApiDraftExportTests` مع `DeviceInboundTelemetryTests`: Failed 0, Passed 9, Skipped 0, Total 9, Duration 1 m 3 s. خروج 0.
- الفلتر الذي يضم `LiveMqttBrokerIntegrationTests` طبع SKIP لاختبار النشر على `127.0.0.1:1883` ثم توقف مضيف الاختبار بلا مقابس. أُوقف العملية. نتيجة 10 ناجحة / 1 متخطى السابقة في هذا اليوم لم تُعد كعملية واحدة مكتملة.

## مراجعة العقد — 2026-11-16

المراجعة الأولى كتبت المقترحات في `docs/gates/G1-contract-decisions.md` قبل الموافقة. القسم التالي يسجل التنفيذ بعد الموافقة. الحكم في ذلك الوقت بقي **G1 BLOCKED**.

## تنفيذ القرارات المعتمدة — 2026-11-17

الحكم لم يتغير: **G1 BLOCKED**. ليست `G1 APPROVED`. لم تُغلق مهام ولم تُحدَّث نسب الإنجاز. `API/appsettings.json` لم يُغيَّر: `Mqtt:Inbound:Enabled=false`، `UseTls=false`، `Mqtt:Outbound:LivePublishEnabled=false`، `Mqtt:Outbox:Enabled=false`. لا Migration. أعمدة نهاية الحادثة كانت موجودة. لا firmware ولا Flutter.

القرارات الثمانية نُفذت على المسودة والخادم: `alarmPhase` على `ALARM_EVENT` و`EVENT_PERSISTED`، إقرار كل مرحلة، تأجيل `SETTINGS_SYNC` وOTA، إبقاء حقلي ACK اختياريين دون اعتبار الغياب إثباتاً، رفض الأوامر البعيدة عند تعذر الوقت كقاعدة جهاز غير مبنية هنا، القيم الابتدائية للاختبار فقط، مبدأ استمرار `stateSequence` بلا آلية Flash مجمّدة، وإبقاء MQTT الإنتاج متوقفاً.

التوافق: غياب `alarmPhase` يعني `ACTIVE`. إقرار قديم بلا مرحلة يقر البداية فقط ولا يمسح صفاً `ENDED`. غياب `confirmedOwnershipVersion` لا يُنسخ من `ownershipVersion`. غياب `clockTrusted` لا يضبط الثقة ولا يمسحها. قيمة مرحلة غير `ACTIVE`/`ENDED` تُرفض بنتيجة `Rejected` ورمز `INVALID_ALARM_PHASE` بلا صف. `queuedPhases` حقل داخلي في صف Outbox الواحد ولا يُنشر.

### ما شُغّل

من `security_backEnd/` مع `NUGET_PACKAGES` و`NUGET_CERT_REVOCATION_MODE=offline`. قاعدة الاختبار PostgreSQL عبر أسرار المستخدم، وليست إنتاجاً.

- محاولة أولى لـ `AlarmEventIngestTests` مع `DeviceInboundTelemetryTests` و`DeviceCommandLifecycleTests`: Failed 1, Passed 33, Total 34, Duration 3 m 21 s. الفشل `Invalid_Alarm_Phase_Is_Rejected`: الاختبار انتظر `BadRequestException` بينما `IngestAsync` يلتقطها ويرجع `Rejected`. أُصلح التوقع ثم أُعيد التشغيل.
- الإعادة: Failed 0, Passed 34, Skipped 0, Total 34, Duration 3 m. خروج 0. تشمل بداية/نهاية وتكرارهما، ونهاية قبل بداية، وبداية بعد نهاية، وإقراراً قديماً بلا مرحلة، و`NoOp` الذي يبقي `ProcessedAt` فارغاً على `EVENT_PERSISTED`، وACK بلا الحقلين الاختياريين.
- `MqttDraftV1SchemaTests` بعد عينات `alarmPhase`: Failed 0, Passed 32, Skipped 0, Total 32, Duration 647 ms. خروج 0. كانت 29 قبل العينات الجديدة.
- `OpenApiDraftExportTests`: Failed 0, Passed 1, Skipped 0, Total 1, Duration 6 s. خروج 0.

محاولة MQTT قبل تشغيل Docker: محرك Docker غير متصل (`npipe:////./pipe/dockerDesktopLinuxEngine` غير موجود). `LiveMqttTlsReliabilityTests` في تلك المحاولة: Passed 1, Skipped 7, Total 8, Duration 65 ms. الناجح الوحيد `Committed_AppSettings_Keep_Tls_And_Live_Publish_Disabled`. السبعة الأخرى تخطت لأن `docker` لم يكن متاحاً. لا تُحسب نجاح TLS.

بعد تشغيل Docker Desktop، إصدار المحرك `29.9.3`، أُضيف مجلد `docker.exe` إلى `PATH` وأُعيد:

- `LiveMqttTlsReliabilityTests`: Failed 0, Passed 8, Skipped 0, Total 8, Duration 2 m 13 s. خروج 0.
- `LiveMqttInboundIntegrationTests`: Failed 0, Passed 7, Skipped 0, Total 7, Duration 4 m. خروج 0.
- `IsolatedMosquittoAclTests`: Failed 0, Passed 7, Skipped 0, Total 7, Duration 56 s. خروج 0. فشل المصادقة العابر في 2026-11-16 لم يتكرر في هذا التشغيل، ويبقى مسجلاً أعلاه ولا يُمحى.
- `LiveMqttBrokerIntegrationTests`: Failed 0, Passed 1, Skipped 1, Total 2, Duration 6 s. خروج 0. المتخطى `Outbox_Dispatch_Delivers_ExecuteCommand_To_Real_Broker_Subscriber` لأن `127.0.0.1:1883` غير متاح. الناجح `Live_Broker_Probe_Documents_Current_Environment` يوثق عدم الوصول ولا يثبت نشراً. هذا التشغيل وحده اكتمل ولم يتوقف المضيف. إثبات النشر السابق على `1883` لم يُعد في هذه الجولة.

### ما لم يُشغَّل في قسم التنفيذ

- قتل PostgreSQL نُقل إلى قسم التحقق النهائي أدناه (حاوية Docker معزولة، وليس خدمة Windows المشتركة).
- لا ESP32، ولا اختبار انقطاع طاقة لـ `stateSequence`، ولا رفض `expiresAt` على العتاد.
- لا عميل Flutter أو Web.
- الفلتر المجمّع الذي توقف في 2026-11-16 لم يُعد كعملية واحدة. الأصناف شُغّلت منفصلة حتى لا يقفل البناء `Domain.dll` ولا تشارك مجموعات الاختبار قاعدة واحدة بالتوازي.

## تحقق Backend النهائي — 2026-11-17

الحكم لم يتغير: **G1 BLOCKED**. ليست `G1 APPROVED` ولا `G0 APPROVED`. لم تُغلق مهام ولم تُحدَّث نسب الإنجاز. لم تُغيَّر إعدادات الإنتاج. لم تُنشأ Migration. لم يُكتب firmware.

### اختبارات جديدة / إعادة تشغيل

من `security_backEnd/` مع Docker في `PATH` و`NUGET_CERT_REVOCATION_MODE=offline`:

- `AlarmEventIngestTests`: Failed 0, Passed 20, Skipped 0, Total 20, Duration 1 m 51 s. خروج 0. يشمل الاختبارين الجديدين `Ended_During_Active_Publish_Keeps_Ended_Phase_On_Outbox` و`Failed_Ended_Publish_Does_Not_Drop_Remaining_Phase`، وإشعار بداية فقط عند `ACTIVE` أولاً، وصفر إشعارات عند `ENDED` أولاً (`End_Before_Start_Is_Stored_And_Later_Start_Does_Not_Reopen`).
- `LiveMqttBrokerIntegrationTests` بعد تشغيل حاوية `ss-g1-anon` على `127.0.0.1:1883` (`allow_anonymous true`): Failed 0, Passed 2, Skipped 0, Total 2, Duration 18 s. خروج 0. النشر الحي لـ `EXECUTE_COMMAND` نجح؛ الأمر بقي `PENDING` والحماية `DISARMED`.
- `IsolatedPostgresKillRecoveryTests` على `postgres:16-alpine` معزول (منفذ عشوائي، قاعدة `esp32_security_pg_kill_test`): المحاولات الأولى فشلت (بدء المضيف قبل الترحيل؛ حمولة تسجيل الجهاز؛ ثم `Open` بعد `docker stop` من دون مسح المجمع). بعد الإصلاح: Failed 0, Passed 1, Skipped 0, Total 1, Duration 46 s. خروج 0. السيناريو: إدخال `ACTIVE` ثم `ENDED`، `docker kill`، فشل `SELECT 1`، `docker start`، بقاء الطابور، ثم نشر المرحلتين.

### مراجعة `MutedAt`

بحث المستودع: لا يوجد كاتب لـ `AlarmEvent.MutedAt` في Application. الحقل يُقرأ فقط في `OwnerDeviceHistoryService`. مسار `MUTE_ALARM` يقبل الأمر ويغلق عبر `COMMAND_ACK` دون تعبئة `MutedAt`. المسؤولية المخططة: إكمال مسار إقرار الأمر ضمن `BE-T-05` (بطاقة `SS-P4-040`)، مع سلوك الصوت على الجهاز في `ESP-T-04`. لا يُنفَّذ هنا؛ ليس نقصاً في عقد `alarmPhase`.

### عوائق ما زالت قائمة

- لا عتاد ESP32، ولا قياس سعة/TTL، ولا استمرار `stateSequence` بعد قطع طاقة حقيقي.
- شهادة الإنتاج وMQTT الحي في `appsettings.json` ما زالا متوقفين عمداً.
- فشل ACL العابر في 2026-11-16 يبقى في السجل رغم نجاح ACL لاحقاً.

## سجل 2026-11-15

## البيئة في 2026-11-15

- الجهاز: Windows، SDK الظاهر في البناء `10.0.302`.
- PostgreSQL عبر Npgsql. اسم القاعدة يُجبَر في `TestDbFactory` إلى قاعدة اختبار (`esp32_security_auth_http_test` لمضيف HTTP). سلسلة الاتصال من User Secrets `esp32-security-backend-api` لأن `ESP32_SECURITY_TEST_DB` غير مضبوط. إصدار الخادم **لم يُستعلم**.
- MQTT: `docker` غير موجود في `PATH` ولا في مسارات Docker Desktop المعتادة. `TCP 127.0.0.1:1883` مغلق. `ESP32_SECURITY_TEST_MQTT_HOST` غير مضبوط.
- ESP32: لا firmware في `security_esp/`. Flutter: لا تطبيق يستهلك العقد في `security_mobile/` أو `admin_web/`.
- لا أسرار أو كلمات مرور broker في هذا التقرير. كلمة مرور Mosquitto، لو وُجد Docker، تُولَّد في الذاكرة فقط.

## أوامر أُعيد تشغيلها والنتائج

من `security_backEnd/`:

```text
dotnet test Application.Tests/Application.Tests.csproj --no-build --filter FullyQualifiedName~MqttDraftV1SchemaTests
```

النتيجة: Failed 0, Passed 29, Skipped 0, Total 29, Duration 1 s. خروج 0. هذه مطابقة JSON Schema للعينات، وليست broker.

```text
dotnet test Application.Tests/Application.Tests.csproj --no-build --filter FullyQualifiedName~OwnerDeviceCommandAcceptanceRulesTests|FullyQualifiedName~OwnerDeviceCommandRulesTests|FullyQualifiedName~AlarmEventAcceptanceRulesTests|FullyQualifiedName~AlarmEventDraftRulesTests|FullyQualifiedName~OwnershipTransferAcceptanceRulesTests|FullyQualifiedName~OwnershipTransferRulesTests|FullyQualifiedName~DeviceConnectivityTests
```

النتيجة: Failed 0, Passed 52, Skipped 0, Total 52, Duration 176 ms. خروج 0. قواعد في الذاكرة، بلا PostgreSQL وبلا MQTT.

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --no-build --filter FullyQualifiedName~DeviceCommandLifecycleTests|FullyQualifiedName~OwnerDeviceCommandHttpTests|FullyQualifiedName~AlarmEventIngestTests|FullyQualifiedName~OwnershipTransferHttpTests|FullyQualifiedName~DeviceOwnershipHttpTests|FullyQualifiedName~DeviceInboundTelemetryTests|FullyQualifiedName~BackendAcceptancePathTests|FullyQualifiedName~LiveMqttBrokerIntegrationTests|FullyQualifiedName~OwnerDeviceStateHttpTests
```

النتيجة: Failed 0, Passed 62, Skipped 1, Total 63, Duration 7 m 32 s. خروج 0.

التخطي الوحيد: `LiveMqttBrokerIntegrationTests.Outbox_Dispatch_Delivers_ExecuteCommand_To_Real_Broker_Subscriber` لأن الوسيط غير متاح. `Live_Broker_Probe_Documents_Current_Environment` نجح كتسجيل لعدم الوصول، وليس كنجاح نشر.

التوزيع: `AlarmEventIngestTests` 13، `OwnershipTransferHttpTests` 11، `DeviceInboundTelemetryTests` 8، `OwnerDeviceCommandHttpTests` 8، `DeviceCommandLifecycleTests` 7، `DeviceOwnershipHttpTests` 7، `OwnerDeviceStateHttpTests` 4، `BackendAcceptancePathTests` 3، `LiveMqttBrokerIntegrationTests` 2 (1 ناجح كـ probe و1 متخطى).

هذه الاختبارات على PostgreSQL. النشر الصادر فيها عبر `RecordingMqttOutboundPublisher` (محاكاة) إلا مسار Live المتخطى. الإدخال يستدعي خدمات الالتقاط مباشرة، لا اشتراك MQTT حي.

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --no-build --filter FullyQualifiedName~IsolatedMosquittoAclTests|FullyQualifiedName~OpenApiDraftExportTests
```

بعد إصلاح غياب `docker` ليصبح تخطياً: Failed 0, Passed 2, Skipped 6, Total 8, Duration 9 s. الستة المتخطاة هي ACL/QoS. الناجحان: probe يوثق غياب Docker، وتصدير OpenAPI.

```text
$env:G1_WRITE_OPENAPI='1'
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --no-build --filter FullyQualifiedName~OpenApiDraftExportTests
```

بعد تعليم `POST` الأمر بـ `[ProducesResponseType(202)]`: Failed 0, Passed 1, Skipped 0, Total 1, Duration 14 s. الملف المكتوب: `docs/contracts/http/openapi.json`. المسار `/api/owner/devices/{deviceId}/commands` عملية `post` تعلن `202` فقط. جسم الرد وحالة `PENDING` ليسا في ملف OpenAPI؛ هما في الكود والاختبار `Post_Arm_Returns_202_Pending_With_Outbox_Without_Claiming_Armed`.

## فصل أنواع الأدلة

| الدليل | ماذا يثبت | ماذا لا يثبت |
|---|---|---|
| `MqttDraftV1SchemaTests` 29/29 | العينات تقبل أو تُرفض حسب مخططات المسودة | أن الـ parser يطابق المخطط بايتاً ببايت، أو أن ESP يرسلها |
| قواعد Application.Tests 52/52 | قواعد القبول في الذاكرة | قاعدة بيانات أو وسيط |
| تكامل PostgreSQL 62 ناجح / 1 متخطى | API وOutbox والالتقاط المباشر وعدم تراجع التسلسل وidempotency وMUTE على الخادم | تسليم Mosquitto أو تنفيذ ESP32 |
| Live MQTT في 2026-11-15 | probe فقط: الوسيط كان غير قابل للوصول | يُستبدل جزئياً بقسم 2026-11-16؛ لا يلغي حد المستهلك الوارد |
| Docker Mosquitto في 2026-11-15 | لم يُشغَّل ذلك اليوم | أُعيد في 2026-11-16؛ انظر قسم الإضافة |
| عتاد ESP32 | لا تشغيل | التسلسل بعد إعادة التشغيل ورفض `expiresAt` بلا ساعة موثوقة |

## معيار G1

- **G1-01 BLOCKED.** وُضعت مسودات MQTT وOpenAPI وآلات الحالات. لا اتفاق ثلاثي، وتبقى تعارضات نهاية الحادثة و`SETTINGS_SYNC` واختيارية حقول ACK.
- **G1-02 BLOCKED.** `D-03` / `D-05` / `NFR-008` تبقى `OPEN`. `HeartbeatTimeoutSeconds=3600` و`CommandTimeoutSeconds=10` قيم كود غير مقاسة كإنتاج.
- **G1-03 BLOCKED.** ACL نصي 7/7، ثم ACL فوق TLS نجح داخل `LiveMqttTlsReliabilityTests`. اتصال TLS بجذر الاختبار نجح، والجذر غير الموثوق وثقة نظام التشغيل رُفضا. الإنتاج يبقى `UseTls=false`. لا شهادة بيئة معتمدة. البند يبقى BLOCKED.
- **G1-04 BLOCKED.** مخططات الأنواع المنفذة نجحت 29/29، لكن نهاية الحادثة بلا مخطط بسبب تعارض المرجع، و`SETTINGS_SYNC` بلا حقول معتمدة. الحقول الزائدة لا يرفضها الـ parser ولا المخطط.
- **G1-05 BLOCKED.** المسار الحي يثبت `202` ثم `PENDING` ثم `EXECUTE_COMMAND` بـ QoS 1 وبلا retain. عامل Outbox أعاد النشر بعد عودة الوسيط ولم يجعل الأمر `EXECUTED`. الأمر المنتهي لم يُنشر. الإنتاج يبقى `NoOpMqttOutboundPublisher`. لا ESP32.
- **G1-06 BLOCKED.** ترتيب ACK/STATE والتكرار وSTATE الأقدم وACK المتأخر وإعادة الاشتراك مثبتة على وسيط بلا persistence. مع `persistence true` و`CleanStart false` و`SessionExpirySeconds=3600`، `STATE` بالتسلسل 5 الذي نُشر أثناء انقطاع المستهلك حُفظ بعد `docker restart`. هذا ليس إعادة تشغيل ESP32، وpersistence الوسيط ليست التخزين المحلي للجهاز.
- **G1-07 BLOCKED.** الحوادث الثلاثة وتكرارها و`EVENT_PERSISTED.stored=true` مثبتة عبر المستهلك. حجز PUBACK أُثبت باستثناء `IOException` محقون قبل المعاملة، لا بقتل PostgreSQL. تعطل القاعدة أمام جهاز حقيقي، وعدم حذف الحدث على الجهاز قبل الإقرار، غير مُثبتين.
- **G1-08 BLOCKED.** `Mute_Requires_Matching_Current_Alarm_Uid` و`TestSiren_Rejected_During_Alarm_StopTest_Requires_Test` نجحا على API/PostgreSQL. تنفيذ الكتم على ESP32 غير مُختبر.
- **G1-09 BLOCKED.** ربط `PENDING` والنقل والإلغاء وعزل API نجحت على PostgreSQL (`DeviceOwnershipHttpTests` 7 و`OwnershipTransferHttpTests` 11). ACL بين جهازين نجح على broker تجريبي في 2026-11-16. `OWNERSHIP_SYNC` الصادر من Backend إلى ذلك الـ broker لم يُشغَّل في هذه الجولة.
- **G1-10 BLOCKED.** لا ممثل ESP ولا ممثل Flutter شغّل اختبار توافق. مشترك MQTTnet التجريبي ليس firmware.
- **G1-11 PASS.** هذا الملف يسجل الأوامر والبيئة والنتائج وأسماء الاختبارات ونسخة المخطط `schemaVersion=1` (JSON Schema draft 2020-12) وقيود التشغيل، بلا أسرار. نجاح التقرير لا يجتاز البنود G1-01…G1-10.

## خريطة الأرقام

- خطة `BE-04`/`BE-05` = `BE-T-04` مغلقة سابقاً. `BE-06`/`BE-07` = `BE-T-05` ما زالت مفتوحة. `BE-08` = `BE-T-06`. نقل الملكية في التنفيذ هو `BE-T-07`. `BE-10` في الخطة = عامل FCM = `BE-T-09`. `BE-11` = `BE-T-10`.
- بوابة العقد: `INT-T-01`…`INT-T-04` ما زالت `[ ]`. لم تُعلَّم في هذه الجولة.
