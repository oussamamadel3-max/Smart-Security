# INT-T-03 — تقرير مراجعة وتوحيد عقد HTTP / OpenAPI

| الحقل | القيمة |
|--------|--------|
| المهمة | `INT-T-03` / `T-03` |
| التاريخ | 2026-11-22 |
| الحكم | **CLOSED** (بعد موافقة المالك) |
| إغلاق رسمي | **مغلق** — `APPROVE TASK CLOSURE: INT-T-03` · انظر `INT-T-03-CLOSURE-REPORT.md` |
| العدّاد بعد الإغلاق | **25/50** · Integration **3/6** |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED` |

---

## 1. النص الرسمي للمهمة ونطاقها

من `integration/integration-development-tasks.md` — **T-03 عقد HTTP / OpenAPI مشترك**:

- **SRS:** §10.3 Flutter↔ASP.NET؛ خطة §4.2
- **المطلوب:** جرد عقد المصادقة المنفذ (login/refresh/logout/logout-all/me وإدارة المالك) مع Web Cookie Refresh+CSRF وMobile Body Refresh؛ توثيق الأجهزة/الربط/النقل/الحالة/الأوامر (202+PENDING+idempotency)/الأحداث/الأعطال/السجلات/push/التنبيه الإداري؛ تمييز المنفذ من المقترح؛ أخطاء 401/403/409/503 ورفض الحساب المعطّل.
- **القبول:** نُشرت مسودة يعتمدها `admin_web` و`security_mobile`؛ POST أمر لا يدّعي تغيّر الحماية.
- **البطاقات:**
  1. جرد وتوثيق OpenAPI المنفذ (Refresh Web/Mobile)
  2. مسودة OpenAPI للمسارات غير المنفذة بوسم `proposed`
  3. جدول أخطاء ورموز ثابتة
  4. قواعد الترقيم/الفرز والملكية مع فصل الدور عن ملكية الجهاز

**خارج النطاق:** تعديل SRS/DB · تفعيل MQTT إنتاجي · عتاد D-01 · تشغيل Mobile على جهاز · INT-T-04/OPS-T-02 · إغلاق G0/G1/G2 · إغلاق ذاتي لـ INT-T-03.

---

## 2. حالة الاعتماديات عند البدء

| اعتماد | الحالة عند البدء |
|--------|------------------|
| INT-T-02 | ✓ مغلقة رسمياً — `INT-T-02-CLOSURE-REPORT.md` |
| Official Progress | 24/50 — 48% |
| Integration | 2/6 |
| Ops / Backend / ESP / Mobile / Web | 1/6 · 5/13 · 0/9 · 8/8 · 8/8 |
| G0 / G1 / G2 / MQTT Prod | NOT CLOSED / BLOCKED / NOT CLOSED / DISABLED |
| OPS-T-01 Compose | ✓ متاح (Postgres+Mosquitto healthy) |

**التحقق:** إغلاق INT-T-02 شرط منطقي مستوفى؛ لا مانع بدء.

---

## 3. جرد عقود HTTP الحالية

- **37** عملية HTTP في **11** controller (`security_backEnd/API/Controllers`).
- OpenAPI: **30** path / **37** method — تغطية مسارات كاملة.
- التفصيل: [`integration/contracts/HTTP-ENDPOINT-INVENTORY.md`](../../integration/contracts/HTTP-ENDPOINT-INVENTORY.md)
- التوحيد: [`integration/contracts/HTTP-V1-DRAFT.md`](../../integration/contracts/HTTP-V1-DRAFT.md)

مجموعات: Health · Auth (5) · Owner devices/commands/alarms/notifications (9) · Push tokens (4) · Admin owners (6) · Admin devices (7) · Admin alerts/faults/audit (5).

---

## 4. مصفوفة Endpoints / DTOs / Status Codes (ملخص)

| فئة | Success codes | أخطاء موثّقة | ملاحظة حرجة |
|-----|---------------|--------------|-------------|
| Auth | 200 | 401/403 | `refreshDelivery` body\|cookie |
| Owner commands POST | **202** | 401/403/409/503 | `actualStateChanged=false` |
| Admin create (owners/devices/link/alerts) | **201** | 401/403/… | صُحّح OpenAPI من 200→201 |
| معظم القراءة | 200 | 401/403/404 | غلاف `ApiResponse` |

DTO أمر القبول: `OwnerDeviceCommandResponseDto` — الحقول كاملة في الجرد والقسم 5 من المسودة.

---

## 5. مقارنة OpenAPI بالتنفيذ

| بند | قبل INT-T-03 | بعد |
|-----|--------------|-----|
| تطابق المسارات | ✓ | ✓ |
| POST commands = 202 | ✓ | ✓ + وصف + schema استجابة |
| 201 لعمليات الإنشاء | ✗ (موثّق 200) | ✓ overlay |
| مخططات طلب | 12 | 12 طلب + استجابة أمر/هوية/خطأ (18 schemas) |
| غلاف خطأ | غائب | `ApiErrorResponse` |
| Cookie/CSRF كنموذج أمان OpenAPI | غير ممثّل | موثّق نصياً في المسودة/`error-codes` (قيود Swashbuckle) |
| مسارات `proposed` | لا | `openapi.proposed.json` |

**لا شرعنة لمخالفات معتمدة:** الفجوات مؤجّلة صراحة؛ MQTT إنتاجي معطّل.

---

## 6. مراجعة توافق Backend · Mobile · Web

محضر: [`INT-T-03-CROSS-PROJECT-REVIEW.md`](../../integration/contracts/INT-T-03-CROSS-PROJECT-REVIEW.md)

| مشروع | الحكم |
|--------|--------|
| Backend | `APPROVE_WITH_GAPS` (مخططات استجابة جزئية) |
| Mobile | `COMPATIBLE` (202≠تنفيذ؛ body refresh) |
| Web | `COMPATIBLE` برمجياً؛ CSRF حي **NOT VERIFIED** |
| Core | مسارات هوية + رموز حالة — by design |

المراجعات تقنية AI — **ليست** توقيعات بشرية. الاعتماد البشري ينتظر موافقة الإغلاق.

---

## 7. الأعمال الفرعية المنفذة

| بطاقة | المخرج |
|-------|--------|
| جرد OpenAPI المنفذ + Refresh | `HTTP-ENDPOINT-INVENTORY.md` + §3 المسودة |
| `proposed` | `openapi.proposed.json` + PROP-HTTP-01…04 |
| أخطاء ورموز | `error-codes.md` محدّث |
| ملكية/ترقيم/دور | §4–6 في `HTTP-V1-DRAFT.md` + جدول admin paging |
| أمثلة | `docs/contracts/http/samples/*` |
| اختبار توافق مسودة | `HttpOpenApiDraftTests.cs` |

---

## 8. الملفات الجديدة والمعدّلة

### جديدة

- `integration/contracts/HTTP-V1-DRAFT.md`
- `integration/contracts/HTTP-ENDPOINT-INVENTORY.md`
- `integration/contracts/INT-T-03-CROSS-PROJECT-REVIEW.md`
- `docs/contracts/http/README.md`
- `docs/contracts/http/openapi.proposed.json`
- `docs/contracts/http/samples/*` (README + 8 JSON)
- `security_backEnd/Application.Tests/HttpOpenApiDraftTests.cs`
- `docs/reports/integration/INT-T-03-report.md`

### معدّلة

- `docs/contracts/http/openapi.json` (info, 201s, command 202 schema, auth security:[], schemas)
- `docs/contracts/http/error-codes.md`
- `integration/contracts/README.md`
- `integration/integration-*.md` (**بدون** `[x]` إغلاق / **بدون** رفع عدّاد)
- `system-context.md`, `system-progress.md` (مؤشر حالة؛ **24/50** ثابت)
- `docs/gates/G1-evidence.md`, `G0-G1-remaining-dependencies.md`

---

## 9. أمثلة العقود

| عيّنة | الغرض |
|-------|--------|
| `auth-login-body-success.json` | Mobile refresh body |
| `auth-login-cookie-success.json` | Web cookie؛ `refreshToken=null` |
| `auth-error-401.json` | `AUTH_UNAUTHENTICATED` |
| `command-post-request.json` | idempotency إلزامي |
| `command-post-202-pending.json` | 202 + PENDING + `actualStateChanged=false` |
| `command-conflict-409.json` | تعارض |
| `alarm-event-active.json` | `status` HTTP ≠ `alarmPhase` MQTT |
| `device-list-item.json` | مؤشرات + ownership |

بلا أسرار حقيقية؛ قيم `REDACTED`.

---

## 10. نتائج الاختبارات

| أمر | البيئة | النتيجة |
|-----|--------|---------|
| `dotnet test Application.Tests … HttpOpenApiDraftTests\|OwnerDeviceCommand*Rules*` | unit | **Failed 0, Passed 33, Skipped 0** |
| `dotnet test API.IntegrationTests … OwnerDeviceCommandHttpTests\|OpenApiDraftExportTests` | PostgreSQL معزول (OPS-T-01 / test DB) | **Failed 0, Passed 9, Skipped 0** |
| `flutter test … device_command_entity_test + command_poll_policy_test` | `security_mobile` | **All tests passed (5)** |
| `flutter test … device_detail_command_guards_test` | `admin_web` | **All tests passed (6)** |

**ملاحظة:** اختبارات التكامل تثبت سلوك HTTP الحي للأوامر (بما فيها `actualStateChanged=false`). اختبارات OpenAPI تثبت المسودة والعينات — **ليست** قبول إنتاج G1.

---

## 11. التعارضات المكتشفة والمعالجة

| تعارض | المعالجة |
|-------|----------|
| OpenAPI يظهر 200 بدل 201 لإنشاء موارد | صُحّح في `openapi.json` |
| `error-codes.md` ادّعى غياب عميل Flutter | أُزيل؛ العملاء موجودون |
| غياب مخطط استجابة الأمر | أُضيف `OwnerDeviceCommandResponseDto` + غلاف |
| `alarmPhase` vs HTTP `status` | وثّق الفرق؛ لا خلط MQTT/HTTP |
| مخططات استجابة ناقصة لبقية DTOs | PARTIAL — لا اختراع حقول؛ PROP-HTTP-03 |

---

## 12. الفجوات المؤجلة وإحالاتها

| فجوة | تصنيف INT-T-03 | إحالة |
|------|----------------|--------|
| S1 / DEP-SAFE-01 | غير مانعة | ESP / G0 |
| DEP-FLUT-01/02 · R-12/R-13 · WEB-EXT-10/11 | غير مانعة | Mobile/Web لاحقة |
| MutedAt كتابة / BE-T-05 | غير مانعة للقراءة | Backend |
| DEVICE_FAULT مسار مالك منفصل | proposed | PROP-HTTP-02 |
| OWNERSHIP_SYNC Outbox إنتاج | خارجي مؤجّل | MQTT DISABLED |
| healthCode UX كامل | غير مانعة | DEP-FLUT-01 |
| عتاد D-01 | خارجي مؤجّل | لا يمنع |
| مراجعة بشرية ثلاثية G1 | معلّقة | INT-T-04 |

---

## 13. مراجعات المشاريع المستقلة

انظر القسم 6 والمحضر. خلاصة: توافق عقدي كافٍ لنشر المسودة المشتركة؛ التحقق الميداني/المتصفح الحي خارج النطاق.

---

## 14. مصفوفة شروط القبول

| شرط | الدليل | التصنيف |
|-----|--------|---------|
| جرد OpenAPI المنفذ + Refresh Web/Mobile | المسودة + الجرد + عينات auth | **VERIFIED** |
| مسودة `proposed` للغير منفّذ | `openapi.proposed.json` | **VERIFIED** |
| جدول أخطاء ورموز | `error-codes.md` | **VERIFIED** |
| ترقيم/فرز/ملكية وفصل دور عن ملكية | HTTP-V1-DRAFT §4–6 | **VERIFIED** (ترقيم مالك = proposed للصفحات) |
| مسودة يعتمدها Mobile/Web | مراجعة توافق + اختبارات 202/PENDING | **VERIFIED** (استهلاك برمجي؛ لا تشغيل جهاز) |
| POST أمر لا يدّعي تغيّر الحماية | DTO + عيّنة + HttpTests + Flutter guards | **VERIFIED** |
| اعتماد G1 / إنتاج MQTT | — | **BLOCKED** (متعمد) |
| موافقة مالك للإغلاق | — | **PENDING** |

---

## 15. أثر المهمة على البوابات

| بوابة | الأثر |
|-------|--------|
| G0 | لا تغيير — NOT CLOSED |
| G1 | لا تغيير — BLOCKED؛ أُضيف دليل توثيقي في `G1-evidence.md` فقط |
| G2 | لا تغيير — NOT CLOSED |
| MQTT Production | يبقى DISABLED |

---

## 16. العدادات الرسمية

| العداد | قبل | بعد التنفيذ (الآن) |
|--------|-----|---------------------|
| النظام | 24/50 | **24/50** |
| Integration | 2/6 | **2/6** |
| Ops | 1/6 | 1/6 |
| Backend | 5/13 | 5/13 |
| ESP | 0/9 | 0/9 |
| Mobile | 8/8 | 8/8 |
| Web | 8/8 | 8/8 |

بعد `APPROVE TASK CLOSURE: INT-T-03` فقط: Integration → 3/6 · النظام → 25/50.

---

## 17. جاهزية INT-T-03 للإغلاق

**READY TO CLOSE**

التنفيذ ضمن نطاق البطاقة مكتمل بأدلة. الإغلاق الرسمي معلّق على موافقة المالك صراحةً. لا تعليم `[x]` قبلها.

---

## 18. جاهزية بدء INT-T-04 و OPS-T-02

| مهمة | بعد INT-T-03 |
|------|----------------|
| INT-T-04 (G1) | اعتمادياً تحتاج إغلاق INT-T-03 + أدلة G1؛ **G1 ما زالت BLOCKED** — لا يُبدأ تلقائياً |
| OPS-T-02 | مستقلة اعتمادياً عن INT-T-03؛ تحتاج `START TASK: OPS-T-02` منفصل |

---

## 19. درس هندسي

**المفهوم:** فصل «تصدير Swagger الخام» عن «عقد تكامل موثّق» عبر overlays صريحة + ملف `proposed` منفصل يمنع خلط المنفَّذ بالمستقبلي.

**لماذا ينجح:** العملاء (Flutter) يعتمدون دلالات حرجة (`202` / `actualStateChanged=false` / قنوات refresh) أكثر من اكتمال كل `$ref`؛ توثيق هذه الدلالات بعينات قابلة للاختبار يمنع انحدار «نجاح HTTP = نجاح حماية».

**متى يُطبَّق:** عند توحيد عقود بين ASP.NET وعميلين يختلفان في جلسة (cookie vs body).

**تحقق عملي:** اختبار يثبت أن عيّنة الأمر تحتوي `actualStateChanged=false` وأن مسار الأوامر في OpenAPI يعلن `202` فقط كقبول.

---

## انتظار المالك

```text
APPROVE TASK CLOSURE: INT-T-03
```

لا انتقال تلقائي إلى INT-T-04 أو OPS-T-02.
