# مهام استكمال برمجي — الويب (Software-Only)

ملف مستقل عن `web-development-tasks.md` (T-01…T-08).  
لا يغيّر إجمالي مهام النظام (50). يُغلق كل بند بعد قبول برمجي مؤكد (`analyze` + `test` + `build web`) دون Firebase/MQTT/G0/G1/نشر.

المرجع البصري: `design/index.html` · المرجع الوظيفي: SRS + عقود Backend · التقارير السابقة: `docs/reports/web/`

تفويض الإغلاق التلقائي: أمر المالك **Autonomous Sequential Web Software Completion** لـ `WEB-C-01`…`WEB-C-06`.

## WEB-C-01 Architecture & Code Quality Audit

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: معمارية Presentation/Domain/Data · Riverpod · GoRouter · Dio · أخطاء · إزالة تكرار/ميت · لا Mock إنتاجي · توافق Web · الحزمة المشتركة.
- القبول: معمارية متسقة · لا تكرار جوهري · analyze/test/build ناجحة · بلا كسر وظائف.
- الدليل: `docs/reports/web/completion/WEB-C-01-report.md` — analyze نظيف · test **81/81** · `flutter build web --release` نجح. سباق نجاح متأخر بعد الخروج أُغلق في WEB-C-02.

## WEB-C-02 Authentication, Session & Authorization Audit

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: Access ذاكرة · Cookie/CSRF · single-flight · 401/403 · عزل Cache · حراسة مسارات · اختبارات race/late.
- القبول: لا ثغرات برمجية معروفة ضمن النطاق · توثيق غير المتحقق حياً.
- الدليل: `docs/reports/web/completion/WEB-C-02-report.md` — analyze نظيف · test **92/92** · `flutter build web --release` نجح. الكوكي الحي CSRF/Origin يبقى **NOT VERIFIED**.

## WEB-C-03 Owner UI & Design Fidelity Audit

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: مطابقة واجهات المالك لـ `design/index.html` · أدلة `docs/reports/web/evidence/owner/` · مصفوفة مقارنة.
- القبول: مراجعة كل شاشات المالك · تصحيح غير المبرر · لا ادّعاء معاينة بلا دليل.
- الدليل: `docs/reports/web/completion/WEB-C-03-report.md` — analyze نظيف · test **99/99** · لقطات الدخول والقائمة. تفاصيل الجهاز والسجل بلا لقطة متصفح (**PARTIAL / NOT VERIFIED** بصرياً).

## WEB-C-04 Admin UI & Design Fidelity Audit

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: مطابقة واجهات الأدمن · توحيد shell/جداول/نماذج · أدلة `evidence/admin/`.
- القبول: اتساق لوحة الإدارة · بلا أوامر تشغيل مالك · أدلة موثّقة.
- الدليل: `docs/reports/web/completion/WEB-C-04-report.md` — analyze نظيف · test **107/107** · `flutter build web --release` نجح. لقطات الإدارة عند 548×594. الشريط العريض والدرج **NOT VERIFIED** في المتصفح (اختبار `navy2` عند 1440). لا تطابق بكسل.

## WEB-C-05 Responsive Design & UX Audit

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: أحجام 360…1920 · حالات Loading/Empty/Error · تنقل · إمكانية وصول · أداء معقول.
- القبول: لا overflow معروف في الأحجام المختبرة · حالات متسقة · suite خضراء.
- الدليل: `docs/reports/web/completion/WEB-C-05-report.md` — analyze نظيف · test **121/121** · `flutter build web --release` نجح. لقطات المتصفح **NOT VERIFIED**. صف السجل الممتلئ عند ≥900 **PARTIAL**.

## WEB-C-06 Final Software Verification & Documentation

- [x] الحالة: مغلقة 2026-11-18 (إغلاق تلقائي)
- المطلوب: تحقق نهائي · تحديث تقارير · WEB-REMAINING-EXTERNAL-WORK · تقرير استكمال نهائي · محاولة WEB-EXT-08/09.
- القبول: حكم جاهزية موثّق · لا PRODUCTION READY بلا تكامل حي.
- الدليل: `docs/reports/web/completion/WEB-C-06-report.md` و`WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md`. analyze نظيف · test **121/121** · `flutter build web --release` نجح. `WEB-EXT-09` مكتمل بتقديم `build/web`. `WEB-EXT-08` يبقى PARTIAL. الحكم **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING**. ليست جاهزية إنتاج.

## تتبّع

| مهمة | الحالة |
|------|--------|
| WEB-C-01 | مغلقة 2026-11-18 |
| WEB-C-02 | مغلقة 2026-11-18 |
| WEB-C-03 | مغلقة 2026-11-18 |
| WEB-C-04 | مغلقة 2026-11-18 |
| WEB-C-05 | مغلقة 2026-11-18 |
| WEB-C-06 | مغلقة 2026-11-18 |
