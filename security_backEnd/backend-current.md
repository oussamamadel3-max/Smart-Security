# المهمة الجارية — باك إند

status: waiting_closure_approval — partial test execution recorded; owner 2026-11-21 doc phase did not close this task
task: BE-T-10 / T-10 — التكامل الآلي وبوابة القبول الخلفية (تنفيذ اختباري جزئي)
srs: معايير قبول القسم 12 (خادم) · AC-T المرتبطة بالباك
parent: يعتمد على تنفيذ T-05/T-06/T-07/T-09 الاختباري — **بلا إغلاق رسمي لها** و**بلا تجاوز G1**
branch: (لم تُنشأ)
worktree: C:/Users/Osama/Desktop/SmartSecurity/security_backEnd

## الحكم الحالي

| البعد | الحالة |
|--------|--------|
| التفويض | صريح سابقاً — تنفيذ اختباري جزئي |
| SS-P9-010 | وحدات قواعد منفَّذة؛ `Application.Tests` **80/80 Passed** (عند آخر تشغيل موثّق في ورقة العمل) |
| SS-P9-020 | PostgreSQL جزئي + انحدار؛ MQTT حي **Skipped/معلّق** بلا Broker/Docker مفوّض |
| SS-P9-030 | `docs/BE_T10_ACCEPTANCE_TRACEABILITY_MATRIX.md` |
| تعارض وثائقي | صُحِّح 2026-11-21 في `backend-development-tasks.md`: لم تعد الحالة «لم تبدأ» |
| G1 / Mosquitto الحي / FCM إنتاجي | **غير منجزين** |
| إغلاق | **ممنوع** دون `APPROVE TASK CLOSURE: BE-T-10`؛ لا `[x]` ولا نسب نهائية |
| ملاحظة نظامية | التسلسل المعتمد التالي على مستوى النظام يبدأ بـ INT-T-01 (لم يُفَوَّض في جولة التوثيق) |

## خارج النطاق الآن

- تعليم `[x]` أو تحديث نسب الإنجاز النهائية.
- بدء INT-T-01/OPS-T-01 من هذه الورقة دون تفويض صريح.
- تفعيل MQTT/FCM إنتاجي أو وضع أسرار في المستودع.
- اعتبار Fake MQTT/FCM أو HTTP 202 أو PUBACK دليلاً على تنفيذ الجهاز.
- تنفيذ فجوة `MutedAt` (مسجّلة تحت BE-T-05/`SS-P4-040`).
