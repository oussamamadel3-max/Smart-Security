# تقرير تثبيت قرارات المالك — G0 / G1

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-21 |
| المصدر | موافقة مالك صريحة بناءً على `docs/reports/integration/G0-G1-DECISION-REVIEW.md` |
| نطاق الجولة | توثيق وتثبيت قرارات فقط |
| الحكم | **`OWNER DECISIONS RECORDED — READY TO START INT-T-01`** |
| G0 | `NOT CLOSED` |
| G1 | `BLOCKED` |
| G2 | `NOT CLOSED` |
| Official Progress | **21/50** (بلا تغيير) |

---

## 1. القرارات المعتمدة من المالك

| المعرّف | القرار المختصر | الحالة الرسمية |
|---------|----------------|----------------|
| D-02 | قاعدة تحقق حادثة الاهتزاز؛ نطاق v1 اهتزاز فقط؛ أرقام التطوير `TEST_ONLY` | `OWNER APPROVED — HARDWARE CALIBRATION PENDING` |
| D-04 | تدوير اعتماد يدوي آمن وموثّق في v1؛ هوية مستقلة؛ لا Wi-Fi password على الخادم؛ لا أسرار في Git | `OWNER APPROVED — MANUAL ROTATION FOR V1` |
| S1 | استعادة آخر إعداد حماية موثوق؛ عند التلف/تعذر الاستعادة استرداد آمن مع إعلان عطل وعدم ادعاء حماية مؤكدة؛ تدخل مالك قبل العودة الطبيعية | `OWNER APPROVED — S1 — VALIDATION PENDING` |
| MQTT/TLS | منع إنتاج غير مشفّر؛ إنتاج معطّل؛ تكامل محلي معزول مسموح بشهادات اختبار؛ لا نقل للاختبار→إنتاج؛ لا استثناء TLS إنتاجي؛ لا تفعيل قبل G1 + بيئة + تفويض | `OWNER APPROVED — ISOLATED LOCAL INTEGRATION ALLOWED — PRODUCTION DISABLED` |
| G1-CD-1…8 | القرارات الثمانية بتاريخ 2026-11-16 تبقى نافذة | نافذة — **ليست** اعتماد بوابة G1 |

المرجع الأساسي للتفاصيل العتادية/S1: `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md`  
مرجع حالة G1/MQTT: `docs/gates/G1-decision.md` + `docs/gates/G1-contract-decisions.md`

---

## 2. القرارات المؤجلة

| المعرّف | ماذا أُجّل | الحالة |
|---------|------------|--------|
| D-01 | الاعتماد النهائي للقطع الإلكترونية وطرازاتها ومواصفاتها الكهربائية؛ BOM يبقى مرجعاً أولياً غير معتمد للشراء/الإنتاج؛ لا إعادة تصميم Firmware بسبب التأجيل؛ D-01 **غير مغلق** | `DEFERRED — HARDWARE DECISION PENDING` |
| D-02 أرقام | N / نافذة T / التبريد النهائية | معايرة معلّقة |
| D-03 إنتاج | TTL الإنتاجي؛ السعة العددية النهائية؛ ميزانية كتابة NVS | تحقق إنتاجي معلّق |
| D-04 آلي | التدوير الآلي للاعتماد | إصدار مستقبلي |
| S1 تحقق | إثبات عتادي/اختباري لسلوك الاسترداد | `VALIDATION PENDING` |
| DEP-* | كتالوج كامل في `docs/gates/G0-G1-remaining-dependencies.md` | متعدد |

---

## 3. القرارات السابقة التي بقيت دون تغيير

- سلامة محلية ومصدر الحقيقة (ACK/STATE).
- D-03 القيم الابتدائية للتطوير: 30ث / 60د / ACK 10ث / تغطية ~ساعة — مع فصل **ACK Timeout** عن **Command TTL**.
- D-05 مبدأ الوقت الموثوق واستمرار `stateSequence`.
- D-06 مبادئ الأرشفة/الاحتفاظ.
- D-07 ASP.NET Core Identity + UUID.
- D-08 OTA مؤجّل.
- D-09 شكل `COMMAND_ACK` (حالة فعلية كاملة + `resultStateSequence`).
- قرارات عقد G1 الثمانية (`alarmPhase`، إقرار المراحل، تأجيل SETTINGS_SYNC، حقول ACK الاختيارية، رفض الأمر بلا وقت، القيم الابتدائية، استمرار التسلسل، إيقاف MQTT الإنتاج).

---

## 4. الملفات المعدّلة ووصف كل تعديل

| الملف | التعديل |
|-------|---------|
| `docs/gates/G0-G1-remaining-dependencies.md` | **جديد** — مرجع مركزي للاعتماديات المؤجلة (لا يدخل عدّاد 50) |
| `docs/gates/G0-decision.md` | تحديث حالة/إضافة مالك؛ G0 تبقى NOT CLOSED |
| `docs/gates/G1-decision.md` | إضافة مالك؛ سياسة MQTT/TLS؛ G1 تبقى BLOCKED |
| `docs/gates/G1-contract-decisions.md` | إضافة إعادة تأكيد 2026-11-21 + فجوة MutedAt |
| `docs/gates/G1-evidence.md` | إضافة: لا أدلة تشغيل جديدة من جولة القرارات |
| `docs/G1_Contract_Decision_and_Acceptance.md` | إحالة لاحقة: `alarmPhase` يستبدل «مخططات متميزة» القديمة |
| `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md` | حالات D-01…D-04 + قسم S1 + BOM غير معتمد + D-08/D-09 |
| `docs/reports/integration/G0-G1-DECISION-REVIEW.md` | إضافة لاحقة لجدول استجابة المالك |
| `AGENTS.md` | فقرة قرارات 2026-11-21 وحدودها |
| `system-context.md` / `system-progress.md` | لقطة حالة + تسلسل تالي؛ **21/50** ثابت |
| `security_esp/esp-context.md` / `esp-current.md` / `esp-progress.md` | قرارات مثبّتة؛ G0 غير مغلق |
| `security_esp/ESP-REMAINING-WORK.md` | رابط للكتالوج المركزي |
| `security_backEnd/backend-context.md` / `backend-current.md` / `backend-progress.md` | محاذاة BE-T-10 وفجوة MutedAt |
| `security_backEnd/backend-development-tasks.md` | تصحيح حالة T-10؛ تسجيل فجوة MutedAt تحت SS-P4-040 |
| `integration/integration-context.md` / `integration-current.md` / `integration-progress.md` | INT-T-01 التالية المعتمدة؛ لم تبدأ |
| `ops/ops-context.md` / `ops-current.md` | OPS-T-01 بعد INT-T-01؛ لم تبدأ |
| `docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md` | هذا التقرير |

**لم تُعدَّل:** SRS، تصميم DB، عقود الرسائل JSON، كود المشاريع، علامات `[x]`، عدّادات المشاريع.

---

## 5. التعارضات الوثائقية التي عُولجت

| التعارض | المعالجة |
|---------|----------|
| BE-T-10 «لم تبدأ» vs تنفيذ جزئي في `backend-current`/التقارير | تصحيح سطر الحالة في `backend-development-tasks.md` إلى تنفيذ اختباري جزئي بلا `[x]` |
| «مخططات متميزة لنهاية الحادثة» في وثيقة 2026-11-15 | إحالة لاحقة إلى `alarmPhase` في `G1-contract-decisions.md` دون إعادة كتابة التاريخ |
| BOM يبدو معتمداً للشراء | تأكيد صريح: مرجع أولي غير معتمد؛ D-01 مؤجّل |
| خلط قيم اختبار بقيم إنتاج | تأكيد `PROVISIONAL` / `TEST_ONLY` وفصل ACK Timeout عن Command TTL |
| خلط قرارات العقد باجتياز G1 | إضافات صريحة في G1-decision / contract-decisions / evidence / AGENTS |
| فجوة `MutedAt` | سُجّلت تحت BE-T-05/`SS-P4-040` و`DEP-BE-MUTED` — **بلا تنفيذ** |

---

## 6. أثر القرارات على كل مشروع

| مشروع | الأثر |
|-------|--------|
| `security_esp` | حالات قرارات محدَّثة؛ S1 موثّق؛ لا إغلاق T-01؛ لا تغيير Firmware في هذه الجولة |
| `security_backEnd` | سياسة MQTT إنتاج معطّل مؤكدة؛ فجوة MutedAt موثّقة؛ محاذاة حالة T-10؛ لا كود |
| `integration` | جاهزية وثائقية لبدء INT-T-01 لاحقاً؛ G1 تبقى BLOCKED |
| `ops` | Compose المحلي مسموح كتكامل معزول؛ لا تفعيل إنتاج؛ OPS-T-01 بعد INT-T-01 |
| `security_mobile` / `admin_web` | لا تغيير نطاق عميل؛ الاعتمادات الخارجية تبقى في الكتالوج |
| النظام | تسلسل مرحلي معتمد؛ ليس PRODUCTION READY |

---

## 7. أعمال ما زالت تحتاج تنفيذاً أو تحققاً

1. تنفيذ **INT-T-01** عند التفويض الصريح.
2. تنفيذ **OPS-T-01** وفق التسلسل.
3. INT-T-02 / INT-T-03 ثم بقية Integration الممكنة برمجياً.
4. تقوية/إغلاق Backend الاختباري (T-05…T-08/T-10) وفق الشروط — بما فيها `MutedAt` لاحقاً.
5. اعتماد قطع D-01 ومعايرة D-02 وقياسات D-03/D-05 وتحقق S1 على عتاد.
6. أدلة G1-01…G1-10 ثم طلب اعتماد البوابة.
7. اعتمادات Firebase/FCM وجهاز الموبايل والويب الحي.

---

## 8. قائمة الاعتماديات المتبقية

المرجع الوحيد للكتالوج: [`../../gates/G0-G1-remaining-dependencies.md`](../../gates/G0-G1-remaining-dependencies.md)

يشمل: DEP-HW-01…07 · DEP-SAFE-01 · DEP-OPS-01/02 · DEP-INT-01/02 · DEP-FCM-01 · DEP-MOB-01 · DEP-WEB-01 · DEP-G0-01 · DEP-G1-01 · DEP-G2-01 · DEP-BE-MUTED.

---

## 9. تأكيد عدم تعديل الكود أو إغلاق البوابات والمهام

- لا تعديل كود في أي مشروع.
- لا `APPROVE TASK CLOSURE` بالنيابة عن المالك.
- لا إغلاق ESP-T-01 ولا أي مهمة أخرى.
- لا إغلاق G0 / G1 / G2.
- لا تعليم `[x]` جديد.
- لا تفعيل MQTT الإنتاجي.
- لا ادّعاء PRODUCTION READY.

---

## 10. تأكيد سلامة العدّادات الرسمية

| العدّاد | القيمة بعد الجولة |
|---------|-------------------|
| النظام | **21/50** |
| Backend | **5/13** |
| ESP | **0/9** |
| Mobile | **8/8** |
| Web | **8/8** |
| Integration | **0/6** |
| Ops | **0/6** |

---

## 11. شروط بدء INT-T-01 وفق المرجع الفعلي

من `integration/integration-development-tasks.md` و`integration-current.md`:

1. مسودة `D-04`/`D-05` من `security_esp` متاحة (محدَّثة بحالات المالك).
2. **لا يلزم** إغلاق G0 أو G1 لكتابة مسودة قابلة للمراجعة.
3. لا يجوز إغلاق المسارات الإنتاجية قبل اعتماد G1.
4. يلزم **تفويض بدء تنفيذ صريح** من المالك لهذه المهمة (هذه الجولة توقفت عند التوثيق).
5. الإغلاق الرسمي لاحقاً يحتاج مراجعة مشتركة و`APPROVE TASK CLOSURE: INT-T-01` وفق قواعد المشروع.

الحكم بعد التوثيق: **`READY TO START INT-T-01`** — التنفيذ لم يبدأ.

---

## 12. درس هندسي مختصر

الفصل بين **تسجيل قرار مالك** و**إغلاق بوابة/مهمة** يمنع تقدماً وهمياً: يمكن اعتماد سياسة S1 أو تدوير يدوي أو قاعدة اهتزاز دون ادعاء أن G0 أُغلقت أو أن المعايرة اكتملت. يُطبَّق ذلك عبر مرجع أساسي واحد لكل قرار (`HARDWARE-DECISIONS` / `G1-contract-decisions`) وكتالوج اعتماديات منفصل عن عدّاد الـ50. التحقق العملي: إن وُجد `OWNER APPROVED` مع `G0 NOT CLOSED` و`21/50` ثابتاً في نفس اليوم، فالمستودع متسق.

---

**نهاية التقرير.**  
الحالة: بانتظار تفويض بدء **INT-T-01**.
