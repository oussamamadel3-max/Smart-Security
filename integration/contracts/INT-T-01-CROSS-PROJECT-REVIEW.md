# محضر مراجعة مشتركة — INT-T-01

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-22 |
| المهمة | `INT-T-01` / مسودة عقد MQTT v1 + آلات الحالات |
| نوع المراجعة | **مراجعة تقنية بواسطة AI Agents** (ثلاث مراجعات مستقلة) |
| موافقة مالك على البديل الإجرائي | **صادرة 2026-11-22** — اعتماد المراجعات التقنية الثلاث + موافقة المالك كافية لشرط المراجعة المشتركة ضمن نطاق INT-T-01 (انظر §10) |
| توقيعات بشرية مستقلة بأسماء أفراد | **غير مسجّلة** — لم تصدر |
| نسخة العقود المراجعة | `docs/contracts/mqtt/v1/` (`schemaVersion=1`) · `docs/contracts/state-machines.md` · `integration/contracts/MQTT-V1-DRAFT.md` |
| مرجع قرارات | `docs/gates/G1-contract-decisions.md` · `docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md` |
| حكم تقني مجمّع | **`APPROVE_WITH_GAPS`** من منظور ESP وBackend وFlutter |
| أثر على الإغلاق الرسمي | أُغلق INT-T-01 كـ **DRAFT ACCEPTED**؛ G1 تبقى BLOCKED |

> **تمييز صريح:** لا أسماء مراجعين بشر ولا توقيعات ولا موافقات مختلقة. أي صف «موافقة» أدناه يبقى `PENDING` حتى يصدر من المسؤول المعني.

---

## 1. نطاق المراجعة

- توافق نص مسودة INT-T-01 مع كود `security_esp/firmware` و`security_backEnd` و`security_mobile`/`admin_web`/`packages/smart_security_app_core`.
- تصنيف الفجوات: **مانعة لقبول مسودة INT-T-01** مقابل **مرحلة لاحقة**.
- **خارج النطاق:** تعديل كود · تفعيل MQTT إنتاجي · عتاد · تشغيل محاكٍ · إغلاق مهمة/بوابة.

---

## 2. نتائج Review A — ESP

**المراجع الفرعية:** استكشاف فعلي تحت `security_esp/firmware/` (topics، wire_json، connectivity، remote_command، SoftAP).

| الحكم التقني للوكيل | `APPROVE_WITH_GAPS` |
|---------------------|---------------------|

**متوافق (أمثلة أدلة):**

- قنوات `devices/{id}/state|events|acks|commands` — `mqtt_topics.c`
- STATE/HEARTBEAT/ALARM_EVENT/`COMMAND_ACK` (D-09) / OWNERSHIP_SYNC / EVENT_PERSISTED / EVENT_LOSS_REPORT — مكوّنات `wire_json` + orchestrator
- `clockTrusted` + رفض أوامر عند عدم الثقة — `remote_command.c`
- SoftAP يرفض نصوص ARM/DISARM — `wifi_provisioning_ap.c`
- غياب `SETTINGS_SYNC` متعمد — `inbound_message.h`

**فجوات غير مانعة لـ INT-T-01:**

| فجوة | إحالة |
|------|--------|
| لا ناشر `DEVICE_FAULT` | ESP-T-05/07 |
| S1: DISARMED عند تلف التخزين بلا `DEVICE_FAULT`/`UNKNOWN` معلن دائماً | DEP-SAFE-01 · تحقق عتادي |
| إثبات قطع طاقة لـ `stateSequence` | DEP-HW-05 · G1-CD-7 |
| Host tests ≠ قبول عتادي | معروف |

---

## 3. نتائج Review B — Backend

**المراجع الفرعية:** `DeviceInboundTelemetryService` · `AlarmEventIngestService` · `OutboxDispatchService` · `OwnerDeviceCommandService` · `DeviceOwnershipService` · `appsettings.json`.

| الحكم التقني للوكيل | `APPROVE_WITH_GAPS` |
|---------------------|---------------------|

**متوافق (أمثلة أدلة):**

- `alarmPhase` ACTIVE/ENDED + غياب=ACTIVE — `AlarmEventIngestService`
- ACK اختياري `clockTrusted`/`confirmedOwnershipVersion` دون استنساخ من `ownershipVersion` — `ParseCommandAck`
- عدم تراجع التسلسل — `IGNORED_STALE_SEQUENCE`
- MQTT إنتاجي معطّل: `Inbound.Enabled=false` · `LivePublishEnabled=false` · `Outbox.Enabled=false` · `NoOpMqttOutboundPublisher`

**فجوات غير مانعة لـ INT-T-01:**

| فجوة | إحالة |
|------|--------|
| `MutedAt` بلا كاتب | BE-T-05 / `SS-P4-040` / `DEP-BE-MUTED` |
| Outbox لا ينشر `PUBLISH_OWNERSHIP_SYNC` حياً رغم إنشاء الصف | BE-T-05/07 قبل مسار broker حي |

**اختبار معاد تشغيله في هذه الجولة:** انظر §7.

---

## 4. نتائج Review C — Flutter (Mobile + Web + Core)

**المراجع الفرعية:** `smart_security_app_core` · view models التفاصيل · ARB · اختبارات الحراس (Web).

| الحكم التقني للوكيل | `APPROVE_WITH_GAPS` |
|---------------------|---------------------|

**متوافق (أمثلة أدلة):**

- لا عميل MQTT مباشر (HTTPS فقط)
- HTTP 202/PENDING لا يقلب مؤشرات الحماية المؤكدة — `device_detail_view_model` + `device_detail_command_guards_test` (Web)
- فصل ملكية PENDING/ACTIVE · كتم يتطلب `currentAlarmEventUid`
- `CommandStatus.pending.isConfirmedExecution == false` — core

**فجوات غير مانعة لـ INT-T-01:**

| فجوة | إحالة |
|------|--------|
| عرض S1: لا قمع «محمي» إذا ظهر ARMED مع FAULT/UNKNOWN | DEP-SAFE-01 · مهام عميل لاحقة |
| `healthCode` غير معروض | FR-025 عرض · لاحق |
| اختبارات حراس الأوامر أقوى على Web من Mobile | DoD عميل |

---

## 5. مصفوفة توافق مشتركة (مختصرة)

| البند | ESP | Backend | Flutter | مانع INT-T-01؟ |
|-------|-----|---------|---------|----------------|
| قنوات + أنواع v1 الأساسية | ✅ جزئي (+DEVICE_FAULT ناقص ناشر) | ✅ parsers/builders | N/A (HTTPS) | لا |
| مصدر الحقيقة / لا تفاؤل HTTP | ✅ مبدأ | ✅ | ✅ | لا |
| alarmPhase | ✅ wire | ✅ ingest | عبر HTTP status | لا |
| clockTrusted | ✅ | ✅ | عرض وقت غير موثوق | لا |
| SETTINGS_SYNC خارج v1 | ✅ | ✅ لا schema | ✅ لا شاشة | لا |
| MQTT إنتاج معطّل | تجريبي | ✅ flags | N/A | لا |
| S1 تمييز/عرض | جزئي | يستقبل STATE/FAULT | عرض مركّب ناقص | لا (مسجّل) |
| MutedAt | N/A | فجوة كاتب | يعرض إن وُجد | لا |
| عتاد / G1 أدلة | معلّق | معلّق | معلّق | لا لقبول المسودة |

---

## 6. مراجع أدلة إضافية

- `integration/contracts/MQTT-V1-DRAFT.md`
- `integration/contracts/INT-T-01-inventory-and-compatibility.md` (حدّث §ب بفجوات المراجعة)
- `docs/gates/G1-evidence.md`
- وكلاء المراجعة: ESP [`1c93f903-2517-45b4-830f-bd264cb60a59`](1c93f903-2517-45b4-830f-bd264cb60a59) · Backend [`83ae1047-02fd-4371-9dd9-ef65af7dfb68`](83ae1047-02fd-4371-9dd9-ef65af7dfb68) · Flutter [`d2b000cd-d14e-4626-b089-8d58ee2a7546`](d2b000cd-d14e-4626-b089-8d58ee2a7546)

---

## 7. الاختبارات الفعلية (هذه الجولة)

| الأمر | النتيجة |
|-------|---------|
| `dotnet test Application.Tests/Application.Tests.csproj --filter FullyQualifiedName~MqttDraftV1SchemaTests` من `security_backEnd/` | **Failed 0, Passed 32, Skipped 0, Total 32** (exit 0) — يُؤكَّد في تقرير الإغلاق |

لم يُنفَّذ: عتاد · محاكٍ موبايل · MQTT إنتاجي · توقيع بشري.

---

## 8. تصنيف الفجوات لقبول INT-T-01

### غير مانعة (تقبل المسودة مع تسجيل)

- S1 تمييز DISARMED / عرض Flutter مركّب  
- MutedAt  
- DEVICE_FAULT ناشر ESP  
- Outbox OWNERSHIP_SYNC الحي  
- تحقق عتادي D-05/S1  
- INT-T-02 (ترتيب ACK/STATE) · INT-T-03 (OpenAPI) · INT-T-04 (G1)

### مانعة لـ `[x]` الرسمي حسب نص البطاقة

- **مراجعة/موافقة مشتركة بشرية** من ممثّلي ESP + Backend + Flutter (أو تفويض مالك صريح يعتبر المراجعة التقنية كافية) — ما زالت معلّقة كهوية بشرية.

لا فجوة تقنية مكتشفة **تناقض نص المسودة** وتوجب `REJECT` أو إعادة فتح قرارات G1 الثمانية.

---

## 9. التوصيات النهائية لكل مراجعة

| الطرف | توصية تقنية AI |
|-------|----------------|
| ESP | `APPROVE_WITH_GAPS` |
| Backend | `APPROVE_WITH_GAPS` |
| Flutter | `APPROVE_WITH_GAPS` |
| مجمّع | المسودة **صالحة للمراجعة البشرية والإغلاق الرسمي بعد موافقة المالك**؛ الفجوات مؤجّلة بوضوح |

---

## 10. جدول الموافقات

| الدور | النطاق | الحالة | تاريخ |
|-------|--------|--------|-------|
| مراجعة تقنية AI — ESP | توافق المسودة من منظور الجهاز | `RECORDED` — `APPROVE_WITH_GAPS` | 2026-11-22 |
| مراجعة تقنية AI — Backend | توافق المسودة من منظور الخادم | `RECORDED` — `APPROVE_WITH_GAPS` | 2026-11-22 |
| مراجعة تقنية AI — Flutter | توافق دلالات العرض/HTTPS | `RECORDED` — `APPROVE_WITH_GAPS` | 2026-11-22 |
| ممثلون بشريون مستقلون (أسماء أفراد) | توقيعات منفصلة | **غير صادرة / غير مسجّلة** | — |
| مالك المشروع | اعتماد المراجعات التقنية كبديل إجرائي + `APPROVE TASK CLOSURE: INT-T-01` | **`APPROVED`** | 2026-11-22 |

**البديل الإجرائي المُطبَّق:** المالك اعتمد صراحةً المراجعات التقنية الثلاث مع موافقته كمالك كافيين لاستيفاء شرط المراجعة المشتركة ضمن نطاق مسودة INT-T-01، دون اختلاق توقيعات أفراد.
