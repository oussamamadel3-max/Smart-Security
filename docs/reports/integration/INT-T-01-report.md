# تقرير INT-T-01 — مسودة عقد MQTT وآلات الحالات

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-21 |
| المهمة | `INT-T-01` / `T-01` |
| التفويض | `START TASK: INT-T-01` (صريح) |
| الحكم | **`IMPLEMENTATION COMPLETE — REVIEW PENDING`** |
| Official Progress | **21/50** (قبل = بعد) |
| Integration | **0/6** (قبل = بعد؛ بلا `[x]`) |
| G0 / G1 / G2 | `NOT CLOSED` / `BLOCKED` / `NOT CLOSED` |

---

## 1. نص المهمة الرسمية ونطاقها

من `integration/integration-development-tasks.md` §T-01:

- **المطلوب:** تثبيت أسماء القنوات، `messageType` (بما فيها EXECUTE_COMMAND وOWNERSHIP_SYNC وEVENT_PERSISTED وSETTINGS_SYNC عندما يُحسم؛ لا خلط مع OTA)، الحقول الإلزامية، QoS، منع retained للأوامر اللحظية، وآلات حالات الاتصال/الحماية/الصفارة/العتاد/الأمر/الملكية.
- **القبول:** وثيقة v1 قابلة للاختبار؛ أمثلة حد أدنى؛ دلالة `clockTrusted` واضحة؛ مراجعة مشتركة قبل `[x]`.
- **البطاقات الفرعية:** (1) قنوات وACL دلالية (2) آلات الحالات الست (3) QoS/إعادة محاولة/تكرار (4) clockTrusted (5) حسم SETTINGS_SYNC لـ v1.
- **اعتماديات البدء:** مسودتا D-04/D-05 من ESP؛ لا يلزم إغلاق G0/G1 لكتابة المسودة.

**خارج النطاق هنا:** INT-T-02/03/04 · OPS-T-01 · تعديل كود منتجات · تفعيل MQTT إنتاجي · MutedAt · إغلاق بوابات.

---

## 2. الاعتماديات التي رُاجعت

| اعتمادية | الحالة عند التنفيذ |
|----------|---------------------|
| D-04 / D-05 في HARDWARE-DECISIONS | متوفرتان ومحدَّثتان بقرارات المالك |
| قرارات مالك 2026-11-21 | مسجّلة؛ READY TO START كان محققاً |
| مسودات `docs/contracts/mqtt/v1/` | موجودة (9 مخططات + عينات) |
| `state-machines.md` | موجودة |
| قرارات G1 الثمانية | نافذة؛ لم تُعاد فتحها |
| G0 مغلق؟ | لا — غير مطلوب للبدء |
| عتاد / جهاز موبايل | غير مطلوب لهذه المهمة |

---

## 3. الأعمال الفرعية المنفَّذة

| بطاقة | ما نُفّذ |
|-------|----------|
| قنوات وACL دلالية | وثّقت في `MQTT-V1-DRAFT.md` §1 مع ربط README العقود |
| آلات الحالات الست | توحيد مرجع + إضافة S1 تحت الحماية في `state-machines.md` |
| QoS / تكرار | §5 في المسودة الموحدة؛ إحالة أدلة G1-evidence |
| clockTrusted | §3 دلالات STATE/ALARM/ACK |
| SETTINGS_SYNC | حسم: **خارج v1**؛ تهيئة محلية D-04 بديلة؛ لا schema |
| ملكية Integration | `integration/contracts/README.md` + توحيد دون نسخ مخططات |
| S1 | تفصيل DISARMED محلي + فجوة تمييز العرض |
| D-05 إحالة | تصحيح إلى `G1-CD-7` |
| TLS طبقات | تمييز تجهيز شهادة / تحقق معزول / دليل G1 / تفعيل إنتاج |
| جرد + توافق | `INT-T-01-inventory-and-compatibility.md` |

---

## 4. مصفوفة العقود والرسائل

انظر الجدول الكامل في `integration/contracts/INT-T-01-inventory-and-compatibility.md` §أ.

ملخص: STATE · HEARTBEAT · EXECUTE_COMMAND · COMMAND_ACK · ALARM_EVENT · EVENT_PERSISTED · OWNERSHIP_SYNC · EVENT_LOSS_REPORT · DEVICE_FAULT — كلها `schemaVersion=1` / `DRAFT`. SETTINGS_SYNC بلا مخطط (متعمد).

---

## 5. مصفوفة التوافق بين المشاريع

انظر §ب في نفس ملف الجرد. أبرز الفجوات المحالة: عتاد التسلسل، MutedAt→BE-T-05، OpenAPI→INT-T-03، محضر ثلاثي→إغلاق INT-T-01/G1.

---

## 6. التعارضات المكتشفة والمعالجة

| تعارض | المعالجة |
|-------|----------|
| إحالة D-05 إلى «بند 6» بدل G1-CD-7 | صُحّحت في HARDWARE-DECISIONS |
| التباس «لا شهادة قبل G1» يمنع تجهيز دليل G1 | طبقات TLS في G1-decision + DEP-OPS-01 |
| S1 بلا تمييز عقدي لسبب DISARMED | فجوة مسجّلة؛ لا حقل جديد |
| مسودات بلا وصي Integration | `integration/contracts/*` |

لا تعارض استدعى قرار مالك جديد لإكمال المسودة القابلة للمراجعة.

---

## 7. القرارات التي بقيت دون تغيير

مصدر الحقيقة · alarmPhase · إقرار المراحل · SETTINGS_SYNC/OTA خارج v1 · حقول ACK الاختيارية · رفض الأمر بلا وقت · قيم مؤقتة · G1-CD-7 تسلسل · MQTT إنتاج معطّل · D-09 · فصل ACK Timeout عن Command TTL · قرارات مالك D-01…D-04/S1.

---

## 8. الملفات المنشأة والمعدَّلة

**جديدة:**

- `integration/contracts/README.md`
- `integration/contracts/MQTT-V1-DRAFT.md`
- `integration/contracts/INT-T-01-inventory-and-compatibility.md`
- `docs/reports/integration/INT-T-01-report.md`

**معدَّلة (توثيق):**

- `docs/contracts/mqtt/v1/README.md`
- `docs/contracts/state-machines.md`
- `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md` (S1 + G1-CD-7)
- `docs/gates/G1-decision.md` (طبقات TLS)
- `docs/gates/G0-G1-remaining-dependencies.md` (DEP-INT-01 / DEP-OPS-01/02)
- `docs/gates/G1-evidence.md`
- `integration/integration-*.md` (context/current/progress/tasks)
- `system-context.md` · `system-progress.md`

**لم يُعدَّل:** كود المشاريع · SRS · تصميم DB · مخططات JSON للرسائل · علامات `[x]` · عدّادات رسمية.

---

## 9. نتائج الاختبارات الفعلية

| الأمر | البيئة | النتيجة |
|-------|--------|---------|
| `dotnet test Application.Tests/Application.Tests.csproj --nologo --filter FullyQualifiedName~MqttDraftV1SchemaTests` | `security_backEnd/` · net10.0 | **Failed 0, Passed 32, Skipped 0, Total 32, Duration 2 s** · exit 0 |

ملاحظات: استعادة NuGet عانت انقطاعات شبكة مؤقتة ثم اكتملت؛ البناء نجح قبل الاختبار.  
**لم يُنفَّذ:** عتاد ESP · تشغيل موبايل · MQTT إنتاج · مراجعة بشرية ثلاثية موقعة.

تحقق وثائقي يدوي: مسارات العقود التسعة موجودة؛ لا `settings-sync.schema.json`؛ لا نسخ مخططات تحت `integration/contracts/` (روابط فقط).

---

## 10. أدلة استيفاء شروط القبول (جزئياً)

| شرط قبول البطاقة | الدليل | التصنيف |
|------------------|--------|---------|
| وثيقة v1 قابلة للاختبار | `MQTT-V1-DRAFT.md` + مخططات + 32 اختبار schema | VERIFIED (مسودة) |
| أمثلة حد أدنى | `docs/contracts/mqtt/v1/samples/` | VERIFIED |
| دلالة clockTrusted | §3 المسودة + state-machines | VERIFIED (وثائقي) |
| البطاقات الخمس منفَّذة توثيقياً | المسودة + tasks notes | PARTIAL — بلا `[x]` بانتظار مراجعة |
| مراجعة مشتركة ESP+BE+Flutter | غير منفَّذة كموافقات | **NOT VERIFIED / PENDING** |

---

## 11. شروط القبول غير المستوفاة

1. **المراجعة المشتركة الموقّعة** — مطلوبة صراحةً قبل تعليم `[x]` في ملف المهام.
2. لذلك لا يُعلن `READY TO CLOSE` رغم اكتمال التنفيذ التوثيقي.

---

## 12. مؤجّل واعتماديات خارجية

- DEP-INT-01 → `IMPLEMENTATION COMPLETE — REVIEW PENDING`
- DEP-INT-02 (T-02…T-04) · DEP-OPS-02 · عتاد · FCM حي · MutedAt · G1/G2

---

## 13. أثر على البوابات

| بوابة | الأثر |
|-------|--------|
| G0 | لا تغيير — `NOT CLOSED` |
| G1 | لا اعتماد — تبقى `BLOCKED`؛ المسودة تخدم مدخل المراجعة فقط |
| G2 | لا تغيير |

---

## 14. العدّادات قبل/بعد

| عدّاد | قبل | بعد |
|-------|-----|-----|
| النظام | 21/50 | **21/50** |
| Integration | 0/6 | **0/6** |
| Backend / ESP / Mobile / Web / Ops | 5/13 · 0/9 · 8/8 · 8/8 · 0/6 | بلا تغيير |

---

## 15. هل يمكن بدء OPS-T-01؟

**نعم من ناحية الاعتماديات الفعلية للمهمة نفسها:** بطاقة OPS-T-01 لا تشترط إغلاق G0/G1 ولا إغلاق INT-T-01؛ مسودة MQTT/ACL الدلالية متاحة للمراجعة.  
**لكن:** تسلسل المالك يضعها بعد INT-T-01، و**هذه الجولة لا تبدأ OPS-T-01**. يلزم تفويض صريح مستقل. DEP-OPS-02 = `READY TO START AFTER EXPLICIT AUTHORIZATION`.

---

## 16. درس هندسي مختصر

**ملكية العقد دون تكرار الملفات:** جعل Integration وصياً عبر فهرس وروابط إلى `docs/contracts/` يمنع نسخ JSON Schema متعارضة بينما يبقي اختبارات Backend التي تقرأ المسار الثابت تعمل (32/32). يُستخدم عند وجود مسودات تقنية سابقة بلا «مجلد ملكية» واضح. التحقق: ملف وصي واحد + عدم وجود مخطط مكرر + نجاح `MqttDraftV1SchemaTests`.

---

**توقف:** بانتظار `APPROVE TASK CLOSURE: INT-T-01` — بلا انتقال تلقائي للمهمة التالية.
