# تقدّم المشروع — العقود والتكامل (`integration`)

آخر تحديث: 2026-11-22  
السياق: [`integration-context.md`](./integration-context.md) · العام: [`../system-progress.md`](../system-progress.md)

## مؤشرات

| المؤشر | القيمة |
|--------|--------|
| المهام الكلية | 6 (T-01…T-06) |
| مكتملة | **3** (INT-T-01، INT-T-02، INT-T-03) |
| جارية | لا — التالية T-04 بانتظار START |
| معلّقة | 3 |
| النسبة | **50%** (3/6) |

## التركيز الحالي

- آخر إغلاق: **INT-T-03** — مسودة HTTP/OpenAPI (`DRAFT ACCEPTED`؛ G1 BLOCKED)
- التالية: **INT-T-04** بانتظار `START TASK: INT-T-04`
- ورقة العمل: [`integration-current.md`](./integration-current.md)
- الإغلاق: [`../docs/reports/integration/INT-T-03-CLOSURE-REPORT.md`](../docs/reports/integration/INT-T-03-CLOSURE-REPORT.md)

## سجل التقدّم

| التاريخ | المهمة | الحدث |
|---------|--------|--------|
| 2026-11-22 | INT-T-01 | **إغلاق رسمي** — DRAFT ACCEPTED؛ 1/6 |
| 2026-11-22 | — | OPS-T-01 أُغلقت (Compose محلي) |
| 2026-11-22 | INT-T-02 | تنفيذ توثيقي + عينات ترتيب؛ schema 45؛ وارد 10 |
| 2026-11-22 | INT-T-02 | **إغلاق رسمي** بموافقة المالك. العدّاد **2/6** · النظام **24/50**. التقرير: `INT-T-02-CLOSURE-REPORT.md`. G1 تبقى BLOCKED. |
| 2026-11-22 | INT-T-03 | تنفيذ مسودة HTTP/OpenAPI + عينات + مراجعة توافق + اختبارات |
| 2026-11-22 | INT-T-03 | **إغلاق رسمي** بموافقة المالك. العدّاد **3/6** · النظام **25/50**. التقرير: `INT-T-03-CLOSURE-REPORT.md`. G1 تبقى BLOCKED. |

## عوائق

- اعتماد G1 (`INT-T-04`) ما زال BLOCKED.
- فجوات مؤجّلة: MutedAt، S1 UX، عتاد، تفعيل MQTT إنتاجي، اكتمال مخططات استجابة OpenAPI لكل DTO (PROP-HTTP-03).

## نقطة التحقق التالية

- [x] إغلاق INT-T-02 بعد موافقة المالك
- [x] إغلاق INT-T-03 بعد موافقة المالك
- [ ] انتظار `START TASK: INT-T-04` (لا بدء تلقائي)

## قواعد التحديث

1. علّم `[x]` فقط بعد القبول وموافقة الإغلاق.
2. لا تخلط مسودة HTTP/OpenAPI باعتماد G1 الإنتاجي.
3. زامن [`../system-progress.md`](../system-progress.md) عند تغيّر العدّ.
