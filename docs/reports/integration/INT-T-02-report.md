# INT-T-02 — تقرير قبول توثيق D-09 وCOMMAND_ACK / STATE

| الحقل | القيمة |
|--------|--------|
| المهمة | `INT-T-02` / `T-02` |
| التاريخ | 2026-11-22 |
| الحكم | **CLOSED** (بعد موافقة المالك) |
| إغلاق رسمي | **مغلق** — `APPROVE TASK CLOSURE: INT-T-02` · انظر `INT-T-02-CLOSURE-REPORT.md` |
| العدّاد بعد الإغلاق | **24/50** · Integration **2/6** |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED` |

---

## 1. نص البطاقة الرسمية ونطاقها

من `integration/integration-development-tasks.md`:

- **المطلوب:** توثيق الحقول والأمثلة النهائية وفق قرار D-09 المحسوم؛ اختبار وصول `STATE` قبل `ACK` وبعده أو غيابه؛ قواعد التكرار والتسلسل؛ عدم استنتاج نجاح الأمر من HTTP/MQTT PUBACK.
- **القبول:** إعادة توثيق القرار في أمثلة JSON وحالات ترتيب الوصول وقواعد تحديث الحالة؛ مراجعتها مع `security_esp` و`security_backEnd`؛ اعتماد التفاصيل النهائية من اختصاص `INT-T-04` (G1).
- **البطاقات:**
  1. توثيق قرار D-09 المحسوم دون فتح خيار تمثيل بديل
  2. أمثلة ترتيب الوصول وحالات التعارض
  3. قواعد تحديث `device_states` على الخادم

**خارج النطاق:** INT-T-03/04 · OPS-T-02 · تفعيل MQTT إنتاجي · عتاد · Flutter على جهاز · تعديل SRS/DB · إعادة فتح قرارات G1 الثمانية · إغلاق بوابات.

---

## 2. الاعتماديات المستوفاة

| اعتماد | الحالة |
|--------|--------|
| INT-T-01 (مسودة MQTT v1) | ✓ مغلقة — DRAFT ACCEPTED |
| OPS-T-01 (Compose محلي) | ✓ مغلقة — متاح إن لزم؛ لم يُشترط وسيط حي لإثبات التوثيق |
| قرار D-09 الوظيفي | ✓ محسوم مسبقاً (لا إعادة فتح) |
| قرارات G1 الثمانية | نافذة؛ لم تُعاد فتحها |

---

## 3. الأعمال الفرعية المنفذة

| بطاقة | المخرج |
|-------|--------|
| توثيق D-09 | `integration/contracts/D-09-COMMAND-ACK-AND-STATE.md` |
| أمثلة ترتيب/تعارض | `docs/contracts/mqtt/v1/samples/ordering/` (S01…S08، 12 JSON) |
| قواعد `device_states` | §2 في وثيقة D-09 (مطابقة `DeviceInboundTelemetryService`) |
| مراجعة توافق | `integration/contracts/INT-T-02-CROSS-PROJECT-REVIEW.md` |
| اختبار مخطط للعينات | توسيع `MqttDraftV1SchemaTests` |
| فهارس | `contracts/README.md` · `MQTT-V1-DRAFT.md` §8 · `mqtt/v1/README.md` · `state-machines.md` |

---

## 4. الملفات المنشأة والمعدّلة

### جديدة

- `integration/contracts/D-09-COMMAND-ACK-AND-STATE.md`
- `integration/contracts/INT-T-02-CROSS-PROJECT-REVIEW.md`
- `docs/contracts/mqtt/v1/samples/ordering/*` (README + 12 JSON)
- `docs/reports/integration/INT-T-02-report.md`

### معدّلة

- `security_backEnd/Application.Tests/MqttDraftV1SchemaTests.cs`
- `integration/contracts/README.md`, `MQTT-V1-DRAFT.md`
- `docs/contracts/mqtt/v1/README.md`, `docs/contracts/state-machines.md`
- `integration/integration-*.md` (حالة بدون `[x]`/عدّاد)
- `docs/gates/G1-evidence.md`, `G0-G1-remaining-dependencies.md`
- `system-context.md`, `system-progress.md` (مؤشر مهمة؛ **23/50** ثابت)

---

## 5. مراجعة توافق الرسائل بين المشاريع

انظر المحضر الكامل. ملخص:

| مشروع | الحكم |
|--------|--------|
| Backend | متوافق مع D-09 والتنفيذ الحالي؛ MQTT إنتاجي معطّل |
| ESP | متوافق عقدياً؛ إثبات عتاد معلّق |
| Flutter | لا MQTT مباشر؛ فجوات UX S1 مؤجّلة |

**`APPROVE_WITH_GAPS`** ضمن نطاق التوثيق — الفجوات لا تمنع إغلاق INT-T-02؛ تمنع G1.

---

## 6. أمثلة السيناريوهات التي جرى اختبارها

| Id | الترتيب / الحالة | تحقق مخطط | تحقق سلوك خادم (انحدار موجود) |
|----|------------------|-----------|--------------------------------|
| S01 | STATE ثم ACK أحدث | ✓ | يغطي منطق APPLIED |
| S02 | ACK ثم STATE مساوٍ | ✓ | stale/equal ignore |
| S03 | ACK فقط | ✓ | ACK يحدّث إن أحدث |
| S04 | STATE=10 ثم ACK=7 | ✓ | `CommandAck_Applies_…_Without_Rewinding` |
| S05 | تكرار ACK بعد إغلاق | ✓ | لا إعادة فتح في الخدمة |
| S06 | LATE_ACK بعد timeout | ✓ | `Late_Ack_After_Timeout_*` (10 اختبارات مجمّعة مع الوارد) |
| S07 | STATE مصالحة بعد timeout | ✓ | `ReconcileTimedOutCommandsAsync` |
| S08 | REJECTED + لقطة + `clockTrusted=false` | ✓ | مسار REJECTED |

---

## 7. نتائج الاختبارات

### مخطط JSON + ترتيب

```text
dotnet test Application.Tests/Application.Tests.csproj --nologo --filter FullyQualifiedName~MqttDraftV1SchemaTests
```

**Failed 0, Passed 45, Skipped 0** (exit 0).

### وارد PostgreSQL (ACK/STATE / Late ACK)

```text
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --nologo --filter "FullyQualifiedName~DeviceInboundTelemetryTests|FullyQualifiedName~DeviceCommandLifecycleTests.Late_Ack"
```

**Failed 0, Passed 10, Skipped 0** (exit 0) — على مزوّد PostgreSQL الاختباري للمشروع.

### غير مُنفَّذ عمداً / غير مطلوب لإغلاق التوثيق

| بند | السبب |
|-----|--------|
| وسيط Mosquitto حي لترتيب الرسائل على السلك | نطاق INT-T-02 توثيقي؛ أدلة broker سابقة في G1-evidence؛ OPS مكدس متاح لاحقاً |
| عتاد ESP32 | محظور بالقيود |
| Flutter على جهاز | محظور |
| تفعيل LivePublish | محظور |

---

## 8. الفجوات المكتشفة والمعالجة

| فجوة | المعالجة في INT-T-02 |
|------|----------------------|
| نقص أمثلة ترتيب ACK/STATE في المسودة | أُنشئت عينات ordering + وثيقة D-09 |
| تشتت قواعد التحديث بين README والكود | وُحِّدت في D-09 §2 مع إحالة للرموز |

لا تغيير دلالي على مخطط إلزامي؛ لم تُفتح بدائل لـ D-09.

---

## 9. الفجوات المؤجّلة وإحالاتها

| فجوة | الإحالة |
|------|---------|
| اعتماد G1 / محضر ثلاثي بشري | INT-T-04 · DEP-G1-01 |
| OpenAPI HTTP | INT-T-03 |
| `MutedAt` كاتب | DEP-BE-MUTED · BE-T-05 |
| S1 UX / healthCode | DEP-FLUT-01 · DEP-SAFE-01 |
| قطع طاقة `stateSequence` | DEP-HW-05 |
| تفعيل MQTT إنتاجي | DEP-OPS-01 طبقة (د) |

---

## 10. مصفوفة شروط القبول والأدلة

| شرط | الدليل | التصنيف |
|-----|--------|---------|
| توثيق D-09 بلا بديل تمثيل | `D-09-COMMAND-ACK-AND-STATE.md` §1 | VERIFIED |
| أمثلة ترتيب/تعارض JSON | `samples/ordering/` S01…S08 | VERIFIED |
| قواعد تحديث `device_states` | D-09 §2 + اختبارات وارد 10 | VERIFIED |
| عدم استنتاج تنفيذ من HTTP/PUBACK | D-09 §2.5 + state-machines | VERIFIED |
| مراجعة مع ESP/Backend | `INT-T-02-CROSS-PROJECT-REVIEW.md` | VERIFIED (تقني AI؛ بشري ثلاثي → T-04) |
| اعتماد نهائي للحقول | خارج النطاق — INT-T-04 | BLOCKED (متوقع) |
| علامات `[x]` / عدّاد | معلّقة على موافقة المالك | — |

---

## 11. أثر المهمة على G0 وG1 وG2

| بوابة | الأثر |
|-------|--------|
| G0 | لا تغيير |
| G1 | لا إغلاق — BLOCKED؛ أُضيفت أدلة توثيق/مخطط في `G1-evidence.md` |
| G2 | لا تغيير |

---

## 12. العدّادات الرسمية قبل وبعد

| مؤشر | قبل | بعد التنفيذ (قبل إغلاق المهمة) |
|------|-----|--------------------------------|
| النظام | 23/50 | **24/50** (عند الإغلاق) |
| Integration | 1/6 | **2/6** (عند الإغلاق) |
| Operations | 1/6 | 1/6 |
| بطاقات T-02 `[x]` | فارغة | **معلَّمة** عند الإغلاق |

---

## 13. جاهزية الإغلاق

**READY TO CLOSE** — بطاقات النطاق الثلاثة موثّقة ومسنودة باختبارات مخطط وانحدار وارد؛ لا فجوة مانعة داخل INT-T-02.

---

## 14. جاهزية بدء المهمة التالية

- بعد `APPROVE TASK CLOSURE: INT-T-02`: يمكن تفويض `START TASK: INT-T-03` (OpenAPI) و/أو `START TASK: OPS-T-02`.
- **لا بدء تلقائي** من هذا التقرير.
- INT-T-04 يبقى بعد T-02+T-03 والأدلة.

---

## 15. درس هندسي مختصر

**المفهوم:** فصل **فضاء التسلسل الواحد** (`stateSequence` / `resultStateSequence`) عن **ترتيب القنوات الزمني** (`state` vs `acks`).

**لماذا يعمل:** الخادم يطبّق أو يتجاهل كل رسالة بمقارنة التسلسل مع المخزَّن؛ لذلك ACK قبل STATE أو بعده يعطي نفس الحالة النهائية إذا تطابقت القيم، وACK الأقدم لا يرجّع الحماية حتى لو وصل متأخراً على السلك.

**متى يُستخدم:** أي بروتوكول جهاز↔سحابة بقنوات متوازية وحاجة لعدم التراجع.

**تحقق:** عينات `samples/ordering/S04-*` + اختبار `CommandAck_Applies_…_Without_Rewinding`؛ مخطط 45/45.

---

## الحكم النهائي

**CLOSED** — 2026-11-22

صدرت الموافقة: `APPROVE TASK CLOSURE: INT-T-02`.  
الحكم الرسمي: `INT-T-02 CLOSED — D-09 DOCUMENTATION ACCEPTED — G1 REMAINS BLOCKED`  
التفاصيل: `INT-T-02-CLOSURE-REPORT.md`.
