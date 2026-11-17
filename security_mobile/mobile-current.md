# المهمة الحالية — الموبايل

status: awaiting_implementation_approval
task: الجلسة وعميل HTTP والنماذج (MOB-T-02) — تحليل فقط
srs: FR-001، BR-001، NFR-002، NFR-003
parent: T-02
branch: (محلي)
worktree: C:/Users/Osama/Desktop/SmartSecurity/security_mobile

## السياق

- `MOB-T-01` و`WEB-T-01` أُغلقتا في 2026-11-17.
- هذه الورقة تصف الخطة المعروضة للموافقة. **التنفيذ لم يبدأ.**
- لا WEB-T-02، ولا تعديل Backend/ESP/عقود، ولا اعتماد G0/G1.

## المطلوب عند الموافقة

1. عميل Auth داخل `security_mobile` على مسارات Backend المنفّذة: `POST /api/auth/login` و`refresh` و`logout` و`logout-all` و`GET /api/auth/me`.
2. `refreshDelivery=body`. Access JWT في الذاكرة. Refresh في التخزين الآمن للنظام. single-flight عند 401 المتزامن.
3. تمييز `AUTH_UNAUTHENTICATED` (401) عن `AUTH_FORBIDDEN` (403). الحساب المعطّل/المؤرشف يرفضه الخادم اليوم كـ 401 `AUTH_UNAUTHENTICATED` بلا رمز HTTP مستقل.
4. استبدال `SessionBootstrapNotifier` الهيكلي بجلسة حقيقية. لا AuthInterceptor داخل الحزمة المشتركة.

## تحقق T-01 الذي أُغلق

- حذف `security_mobile/app_core` و`admin_web/app_core` بعد التأكد من عدم وجود `package:app_core` في التطبيقين.
- 2026-11-17: الحزمة analyze نظيف وtest **12/12**؛ الموبايل analyze نظيف وtest **1/1**؛ الويب analyze نظيف وtest **1/1**.
- APK: `build/app/outputs/flutter-apk/app-dev-debug.apk` (dev / android-arm64) من البناء السابق.
- تشغيل الجهاز/المحاكي وiOS: مؤجّلان بموافقة المالك — `NOT VERIFIED`.
