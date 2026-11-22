# تقدّم المشروع — التشغيل والبيئات (`ops`)

آخر تحديث: 2026-11-22  
السياق: [`ops-context.md`](./ops-context.md) · العام: [`../system-progress.md`](../system-progress.md)

## مؤشرات

| المؤشر | القيمة |
|--------|--------|
| المهام الكلية | 6 (T-01…T-06) |
| مكتملة | **2** — **بلا تغيير حتى موافقة OPS-T-03** |
| جارية | **OPS-T-03** برمجي مكتمل — EXTERNAL VALIDATION PENDING |
| معلّقة | 4 (بما فيها T-03 حتى الإغلاق) |
| النسبة | **~33%** (2/6) — العداد الرسمي ثابت |

## التركيز الحالي

- جارية: **OPS-T-03** — قوالب Staging + probes + تحقق محلي؛ HTTPS حي معلّق
- ورقة العمل: [`ops-current.md`](./ops-current.md)
- التقرير: [`../docs/reports/ops/OPS-T-03-report.md`](../docs/reports/ops/OPS-T-03-report.md) · التكميلي: [`../docs/reports/ops/OPS-T-03-supplement-report.md`](../docs/reports/ops/OPS-T-03-supplement-report.md)

## سجل التقدّم

| التاريخ | المهمة | الحدث |
|---------|--------|--------|
| 2026-11-22 | T-01 | **إغلاق OPS-T-01** |
| 2026-11-22 | T-02 | **إغلاق OPS-T-02** — Ops **2/6** · النظام **26/50** |
| 2026-11-22 | T-03 | تنفيذ قوالب staging + live/ready + Test-OpsT03؛ HTTPS حي NOT VERIFIED. **2/6 و26/50 ثابتان**. |

## عوائق

- مضيف staging عام + شهادة حقيقية غير متوفرين (DEP-OPS-03 / DEP-WEB-01).
- G1 BLOCKED؛ MQTT إنتاجي DISABLED.
- بناء/إقلاع صورة API staging الكامل لم يُنفَّذ في جلسة التنفيذ (compose config ✓).

## نقطة التحقق التالية

- [x] بدء OPS-T-03 بتفويض START
- [ ] `APPROVE TASK CLOSURE: OPS-T-03` (لا بدء T-04 تلقائياً)
