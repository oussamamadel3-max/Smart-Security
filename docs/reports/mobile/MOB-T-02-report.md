# تقرير المهمة MOB-T-02 — الجلسة وعميل HTTP والنماذج

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | MOB-T-02 / T-02 |
| الاسم | الجلسة وعميل HTTP والنماذج |
| الحالة | **CLOSED** — بموجب `APPROVE TASK CLOSURE: MOB-T-02` (2026-11-17) |
| التاريخ | 2026-11-17 |
| المشروع | `security_mobile` |

## 1. الملخص التنفيذي

نُفّذت جلسة مالك حقيقية على عقد Backend T-02 الفعلي (`refreshDelivery=body`): Access JWT في الذاكرة فقط، Refresh في التخزين الآمن، AuthInterceptor بـ single-flight وتمييز 401/403، شاشة دخول من `design/index.html`، حراسة مسارات، ونماذج حالة أجهزة/أوامر دلالية. أُغلقت رسمياً بعد `APPROVE TASK CLOSURE: MOB-T-02` مع إعادة تحقق 14/14 عند الإغلاق.

## 2. المتطلبات التي نُفذت

| SRS / قيد | التنفيذ |
|-----------|---------|
| FR-001 / BR-001 | دخول حقيقي + حراسة المسارات |
| NFR-002 | HTTPS عبر Dio؛ لا تسجيل رموز/كلمات مرور (logger بدون أجسام) |
| NFR-003 (جانب العميل) | مسح الجلسة عند فشل التجديد/الخروج؛ دور OWNER للواجهة فقط |
| Access في الذاكرة / Refresh آمن | `AccessTokenHolder` + `SecureRefreshTokenStore` |
| Single-flight + 401 متزامن | `AuthInterceptor` + اختبارات |
| 403 ≠ تسجيل خروج | مُثبت بالاختبار |
| منع استعادة بعد logout متأخر | generation + `_logoutInProgress` |

## 3. البطاقات المكتملة

- [x] MOB-02 عميل API + نماذج مشتركة المعنى مع الويب
- [x] شاشة الدخول وحفظ/مسح الجلسة
- [x] حراسة المسارات وإعادة المحاولة الآمنة للجلب
- [x] Refresh single-flight وتخزين دوّار آمن + اختبارات 401

## 4. الملفات المضافة

### `security_mobile`

- `lib/core/domain/device_status_codes.dart`
- `lib/core/session/access_token_holder.dart`
- `lib/core/session/refresh_token_store.dart`
- `lib/core/session/jwt_claims_reader.dart`
- `lib/core/session/auth_session_state.dart`
- `lib/core/session/auth_session_controller.dart`
- `lib/core/network/app_endpoints.dart`
- `lib/core/network/auth_interceptor_keys.dart`
- `lib/core/network/auth_interceptor.dart`
- `lib/core/network/auth_dependency_providers.dart`
- `lib/core/network/mobile_network_providers.dart`
- `lib/core/router/auth_guard.dart`
- `lib/features/auth/domain/**`
- `lib/features/auth/data/**`
- `lib/features/auth/presentation/pages/login_page.dart`
- `lib/features/auth/presentation/viewmodels/login_view_model.dart`
- `test/core/**` (interceptor, session, guard, jwt, status codes)

### تقارير

- `docs/reports/mobile/MOB-T-02-report.md`

## 5. الملفات المعدلة

- `security_mobile/pubspec.yaml` — `dio`, `flutter_secure_storage`
- `security_mobile/lib/main.dart` — override `dioProvider` → `mobileDioProvider`
- `security_mobile/lib/core/router/app_router.dart`
- `security_mobile/lib/features/bootstrap/presentation/pages/splash_page.dart`
- `security_mobile/lib/features/bootstrap/presentation/pages/home_shell_page.dart`
- `security_mobile/test/widget_test.dart`
- `security_mobile/mobile-current.md`
- `packages/smart_security_app_core/lib/src/translations/l10n/app_*.arb` (+ generated)

## 6. الملفات المحذوفة

- `lib/features/auth/presentation/pages/login_placeholder_page.dart` — استُبدل بـ `LoginPage`
- `lib/core/session/session_bootstrap.dart` — استُبدل بـ `authSessionProvider`

## 7. آلية التنفيذ

1. Auth datasource على bare Dio (بدون interceptor) لتفادي حلقات refresh.
2. `AuthSessionController` يملك الحالة الوحيدة؛ الحارس `authGuardRedirect` هو مسار التنقل الوحيد عند انتهاء الجلسة.
3. `AuthInterceptor` على Dio الميزات: Bearer + single-flight + إعادة محاولة إن تغيّر الـ Access مسبقاً + لا معالجة لـ 403 كخروج.
4. استعادة الجلسة عند الإقلاع عبر Refresh المخزَّن ثم دخول للـ home أو login.
5. رفض حساب غير OWNER على العميل (UX) مع الإبقاء على تفويض الخادم كمصدر حقيقة.

## 8. القرارات المعمارية والتقنية

| قرار | السبب |
|------|--------|
| Access في الذاكرة فقط (عكس بعض قواعد Flutter العامة) | بطاقة MOB-T-02 وسياق الموبايل المعتمدان |
| لا AuthInterceptor في الحزمة المشتركة | فصل Mobile body refresh عن Web Cookie/CSRF |
| نماذج أجهزة/أوامر محلية في الموبايل | عقد HTTP المشترك للأجهزة لم يُعتمد بالكامل في integration |
| حقل الدخول = `userName` لا البريد | عقد Backend الفعلي؛ التصميم HTML كان تجريبياً |

## 9. الاختبارات المضافة

- `auth_interceptor_test.dart` — single-flight، 403، logout race، skip loop
- `auth_session_controller_test.dart` — login/store، رفض ADMIN، late refresh بعد logout
- `auth_guard_test.dart`
- `jwt_claims_reader_test.dart`
- `device_status_codes_test.dart`
- تحديث `widget_test.dart` لشاشة الدخول

## 10. نتائج الاختبارات

| الأمر | النتيجة |
|--------|---------|
| `flutter test` (`security_mobile`) | **14/14 passed** |
| `flutter analyze` (`security_mobile`) | **No issues found** |
| `flutter test` (`smart_security_app_core`) | **12/12 passed** |
| `flutter analyze` (`smart_security_app_core`) | **No issues found** |
| `flutter analyze` (`admin_web`) | **No issues found** |

## 11. نتيجة Flutter Analyze

`security_mobile`: نظيف.

## 12. نتيجة بناء APK

```
flutter build apk --debug --flavor dev --target-platform android-arm64
√ Built build\app\outputs\flutter-apk\app-dev-debug.apk
```

exit code 0 · مدة Gradle تقريباً 363.7s.

## 13. جدول تحقق شروط القبول

| الشرط | التصنيف | دليل |
|--------|---------|------|
| تجديد Access مرة واحدة للطلبات المتزامنة | PASSED | `auth_interceptor_test` single-flight |
| استخدام الرمز الجديد بعد الحفظ | PASSED | اختبار + `AccessTokenHolder`/`RefreshTokenStore` |
| فشل التجديد يمسح الحساس ويعود للدخول | PASSED | `markSessionExpired` / `_hardClear` + guard |
| 403 لا يُعامل كخروج | PASSED | اختبار 403 |
| enum مجهول ≠ نجاح | PASSED | `device_status_codes_test` |
| لا تسرّب رموز في السجلات | PASSED | pretty logger بدون bodies؛ لا طباعة tokens |
| شاشة دخول + حراسة | PASSED | widget test + auth_guard tests |
| دور OWNER على العميل فقط | PASSED | رفض ADMIN في session test |
| تشغيل على جهاز/محاكٍ حقيقي | NOT VERIFIED | قرار المالك — لا Emulator/جهاز الآن |
| بناء iOS | NOT VERIFIED | مؤجّل على Windows |

## 14. الأخطاء المكتشفة والمعالجة

1. **QueuedInterceptor + dio.fetch** → جمود محتمل؛ التحويل إلى `Interceptor` عادي.
2. **سباق single-flight التسلسلي** → إعادة المحاولة إن تغيّر Access مسبقاً بدون refresh جديد.
3. **Parsing Authorization كـ List** → `_bearerToken` يوحّد الشكل.
4. **`encryptedSharedPreferences` غير موجود في flutter_secure_storage 11** → استخدام الافتراضي الآمن.
5. **`ref.mounted` غير متاح في Riverpod 2.6 Notifier** → حُذف.

## 15. القيود والفجوات والأعمال المؤجلة

- لا استدعاء endpoints أجهزة/أوامر (نطاق T-03+ / G1).
- لا تحقق ميداني على جهاز حقيقي.
- إغلاق المهمة معلّق على `APPROVE TASK CLOSURE: MOB-T-02`.
- التفويض المسبق بالإغلاق التلقائي **متعارض** مع `AGENTS.md` §6 و`task-execution.mdc` §6.

## 16. نسبة الإنجاز

| النطاق | قبل | بعد (بعد إغلاق رسمي فقط) |
|--------|-----|---------------------------|
| الموبايل | 1/8 (12.5%) | **2/8 (25%)** بعد الإغلاق |
| النظام | 7/50 (14%) | **8/50 (16%)** |

## 17. المهمة التالية واعتمادياتها

- **MOB-T-03** — قائمة الأجهزة وحالة الربط.
- تعتمد على: إغلاق MOB-T-02 ✓؛ مراجعة عقود/OpenAPI أجهزة Backend الفعلية.
- الحالة: مؤهلة وبانتظار موافقة التنفيذ — **لم تُبدأ تلقائياً**.

## الدرس الهندسي المستفاد

- **المفهوم:** Single-flight token refresh مع إعادة محاولة عند اكتشاف Access مُدوَّر مسبقاً.
- **كيف يعمل:** Future مشترك للـ refresh؛ وإن وصلت 401 بعد اكتمال تدوير قام به طلب آخر، يُعاد الطلب بالرمز الجديد دون refresh ثانٍ.
- **لماذا هنا:** طلبات متزامنة ومتسلسلة بعد انتهاء Access يجب ألا تُطلق سلسلة refresh/replay.
- **متى يُعاد استخدامه:** أي عميل JWT قصير العمر مع Refresh دوّار.
- **مثال:** `AuthInterceptor._singleFlightRefresh` + فحص `failedToken != currentToken`.
- **تحقق عملي:** اختبار ثلاثة GET متزامنة تتوقع `refreshCalls == 1` ونجاح الإعادة.
