# المهمة الحالية — العقود والتكامل

status: ready_to_start_pending_explicit_authorization — documentation phase complete; execution not started
task: مسودة عقد MQTT وآلات الحالات (مدخل بوابة G1)
srs: مكمل FR-006 FR-021 FR-034 — NFR-002 NFR-004 NFR-005
parent: INT-T-01 / shared MQTT contract draft
branch: (لم تُنشأ بعد)
worktree: C:/Users/Osama/Desktop/SmartSecurity/integration

## سياق ما بعد قرارات المالك (2026-11-21)

- تثبيت القرارات: [`../docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md`](../docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md)
- الاعتماديات: [`../docs/gates/G0-G1-remaining-dependencies.md`](../docs/gates/G0-G1-remaining-dependencies.md)
- G0 = NOT CLOSED · G1 = BLOCKED · Official Progress = 21/50
- التسلسل المعتمد: INT-T-01 → OPS-T-01 → INT-T-02 → INT-T-03 → تقوية Backend المستقلة…
- **هذه الجولة لم تبدأ تنفيذ INT-T-01.** بانتظار تفويض بدء صريح.

## شروط بدء INT-T-01 (حسب المرجع الفعلي)

من `integration-development-tasks.md`:

- تعتمد على مسودة `D-04`/`D-05` على الأقل من `security_esp` (موجودة ومحدَّثة بحالات المالك).
- **لا يلزم إغلاق G0 أو G1** لكتابة مسودة قابلة للمراجعة.
- لا يجوز إغلاق المسارات الإنتاجية قبل اعتماد G1.
- بطاقات INT-T-01 تُعلَّم `[x]` فقط عند الإغلاق بعد القبول — ليست جزءاً من جولة التوثيق الحالية.

## المطلوب

1. مسودة وثيقة عقد MQTT v1: قنوات، ناشر/مشترك، `messageType`، حقول إلزامية، QoS، منع retained للأوامر اللحظية.
2. توصيف آلات الحالات للمراجعة المشتركة.
3. أمثلة الحد الأدنى وفق مبدأ `D-09` وقرارات `alarmPhase` المعتمدة.
4. مشاركة المسودة للمراجعة الأولية (الاعتماد الرسمي في `INT-T-04`).
5. دلالة `SETTINGS_SYNC` (مؤجّل خارج v1 بقرار معتمد) مفصولة عن OTA.
6. بيان اعتماديات `D-04`/`D-05`/`S1` وعدم تجميدها قبل التحقق العتادي.

## غير مطلوب في هذه المهمة

- اعتماد G1 النهائي أو OpenAPI الكامل (`T-03`/`T-04`).
- تنفيذ عمال MQTT أو شاشات.
- مصفوفة G2 (`T-05`).
- تفعيل MQTT الإنتاجي.

## تم

_(فارغ — المهمة لم تبدأ التنفيذ؛ جولة 2026-11-21 توثيق قرارات فقط)_

## التحقق

_(بعد التنفيذ: مسار الوثيقة + قائمة مراجعين)_
