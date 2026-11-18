# تقرير المهمة WEB-T-02 — عميل API والنماذج والدخول

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | WEB-T-02 / T-02 |
| الاسم | عميل API والنماذج والدخول |
| الحالة | **CLOSED** — بموجب تفويض المالك المسبق المشروط: Autonomous Sequential Web Development (2026-11-17) |
| التاريخ | 2026-11-17 |
| المشروع | `admin_web` |

## 1. الملخص التنفيذي

نُفّذت جلسة ويب حقيقية على عقد Backend T-02 (`refreshDelivery=cookie`): Access JWT في الذاكرة فقط، Refresh في HttpOnly Cookie، CSRF Cookie↔`X-CSRF-TOKEN`، `BrowserHttpClientAdapter.withCredentials`، AuthInterceptor بـ single-flight، توجيه OWNER/ADMIN، وشاشة دخول من `design/index.html`. نماذج الحالات الدلالية نُقلت إلى `packages/smart_security_app_core`. أُغلقت بعد تحقق القبول البرمجي الإلزامي.

## 2. المتطلبات التي نُفذت

| SRS / قيد | التنفيذ |
|-----------|---------|
| FR-001 / BR-001 | دخول حقيقي + حراسة المسارات + توجيه الدور |
| NFR-002 | HTTPS عبر Dio؛ لا تسجيل رموز/كلمات مرور |
| NFR-003 (عميل) | مسح Access عند فشل التجديد/الخروج؛ فصل مساري مالك/أدمن |
| Access ذاكرة / Refresh Cookie | `AccessTokenHolder` + cookie HttpOnly |
| CSRF double-submit | `CsrfHeaderInterceptor` + `BrowserCsrfTokenReader` |
| Single-flight + 401 | `AuthInterceptor` + اختبارات |
| 403 ≠ خروج | مُثبت بالاختبار |
| enum مجهول ≠ نجاح | `device_status_codes` في الحزمة المشتركة |

## 3. البطاقات المكتملة

- [x] WEB-02 نماذج وحالات وأخطاء مشتركة
- [x] شاشة الدخول + Access بالذاكرة وRefresh HttpOnly Cookie + CSRF/Origin + استعادة آمنة بعد reload ومسح الجلسة
- [x] توجيه حسب الدور و401/403 والتعطيل + single-flight Refresh ومنع تكرار طلبات الكتابة دون idempotency

## 4. الملفات المضافة

### `admin_web`

- `lib/core/session/*` (access, jwt, csrf, auth session)
- `lib/core/network/*` (auth interceptor, CSRF, endpoints, web Dio)
- `lib/core/router/auth_guard.dart`
- `lib/features/auth/**` (domain/data/presentation)
- `test/core/**` و`test/widget_test.dart`

### `packages/smart_security_app_core`

- `lib/src/domain/device_status_codes.dart`
- `test/device_status_codes_test.dart`

### تقارير

- `docs/reports/web/WEB-T-02-report.md`

## 5. الملفات المعدلة

- `admin_web/pubspec.yaml` — `dio`
- `admin_web/lib/main.dart` — credentials adapter + `webDioProvider`
- `admin_web/lib/core/router/app_router.dart`
- `admin_web/lib/features/bootstrap|owner|admin` pages
- `packages/smart_security_app_core/lib/smart_security_app_core.dart`
- ملفات تخطيط الويب والنظام

## 6. الملفات المحذوفة

- `lib/core/session/session_bootstrap.dart` — استُبدل بـ `authSessionProvider`
- `lib/features/auth/presentation/pages/login_placeholder_page.dart` — استُبدل بـ `LoginPage`

## 7. آلية التنفيذ

1. Auth datasource على bare Dio مع `withCredentials` وCSRF attach دون AuthInterceptor.
2. Login يطلب `refreshDelivery=cookie`؛ الخادم يضبط `esp32_rt` (HttpOnly) و`esp32_csrf`.
3. Refresh/Logout يرسلان CSRF header؛ المتصفح يرسل Refresh cookie تلقائياً.
4. استعادة بعد Reload: وجود CSRF readable → refresh → Access في الذاكرة أو الدخول.
5. الحارس يوجّه ADMIN إلى `/admin` وOWNER إلى `/owner`؛ لا خلط افتراضي.

## 8. القرارات المعمارية والتقنية

| قرار | السبب |
|------|--------|
| لا تخزين Refresh في JS / Secure Storage | عقد الويب = HttpOnly Cookie |
| CSRF reader قابل للحقن | اختبارات VM بلا `document.cookie` |
| نماذج الحالة في الحزمة المشتركة | معنى مشترك مع الموبايل دون اعتماد على `security_mobile` |
| قبول OWNER وADMIN | تطبيق الويب يخدم الدورين |

## 9. الاختبارات المضافة

- `auth_interceptor_test.dart` — single-flight / 403 / logout race / skip
- `csrf_header_interceptor_test.dart`
- `auth_session_controller_test.dart` — login، ADMIN، رفض أدوار، late refresh، restore بدون CSRF
- `auth_guard_test.dart`
- `jwt_claims_reader_test.dart`
- تحديث `widget_test.dart`

## 10. نتائج جميع الاختبارات

| الأمر | النتيجة |
|--------|---------|
| `flutter test` (`admin_web`) | **20/20 passed** |
| `flutter analyze` (`admin_web`) | **No issues found** |
| `flutter test` (`smart_security_app_core`) | **13/13 passed** |
| `flutter analyze` (`smart_security_app_core`) | **No issues found** |

## 11. نتيجة Flutter Analyze

`admin_web`: نظيف.

## 12. نتيجة بناء Web

```
flutter build web --release
√ Built build\web
```

(~264.8s compile)

## 13. جدول تحقق شروط القبول

| الشرط | التصنيف | دليل |
|--------|---------|------|
| تجديد Access مرة واحدة للطلبات المتزامنة | PASSED | `auth_interceptor_test` |
| Access في الذاكرة فقط | PASSED | session tests |
| لا رموز في localStorage/sessionStorage | PASSED | لا كود تخزين رموز؛ Cookie فقط |
| CSRF header عند refresh | PASSED | `csrf_header_interceptor_test` |
| 403 لا يُعامل كخروج | PASSED | interceptor test |
| توجيه ADMIN/OWNER | PASSED | auth_guard tests |
| enum مجهول ≠ نجاح | PASSED | core `device_status_codes_test` |
| CSRF/Origin مرفوض على خادم حي | NOT VERIFIED | يتطلب Backend + متصفح حي |
| CORS/Credentials إنتاجي | NOT VERIFIED | Software-Only |
| تعطيل حساب على خادم حي | NOT VERIFIED | يحتاج بيئة Backend |

## 14. الأخطاء المكتشفة وكيفية معالجتها

1. `dart:html` nullable `document.cookie` → معالجة `?? ''` + ignore web-library infos.

## 15. القيود والفجوات والأعمال المؤجلة

- اختبار CSRF خاطئ / Origin أجنبي ضد Backend حي → `WEB-REMAINING-EXTERNAL-WORK.md`.
- لا استدعاء endpoints أجهزة (نطاق T-03+).

## 16. نسبة إنجاز الويب والمشروع

- ويب: **2/8 = 25%**
- نظام: **15/50 = 30%** (بعد إغلاق WEB-T-02)

## 17. حالة المهمة التالية

- **WEB-T-03** جاهزة الاعتماد على T-02؛ تعتمد على Owner Devices API الفعلي.

## 18. عقود API المستخدمة

- `POST /api/auth/login` (`refreshDelivery=cookie`)
- `POST /api/auth/refresh` (Cookie + CSRF)
- `POST /api/auth/logout` / `logout-all`
- `GET /api/auth/me`
- التكامل الحي: **NOT VERIFIED** في هذه الجولة (اختبارات معزولة + مطابقة كود Backend)

## 19. نتائج اختبارات المتصفح

- بناء Web ناجح؛ جلسة تفاعلية حية: **NOT VERIFIED**.

## الدرس الهندسي المستفاد

- **المفهوم:** فصل قناة Refresh (Body مقابل Cookie/CSRF) مع الإبقاء على نفس نمط single-flight للـ Access.
- **كيف يعمل:** الويب لا يقرأ Refresh؛ يعتمد على Cookie HttpOnly + CSRF غير HttpOnly مزدوج الإرسال.
- **لماذا هنا:** متطلبات NFR وأمن المتصفح تمنع تخزين Refresh في JS.
- **متى يُعاد استخدامه:** أي SPA تحتاج جلسة دوّارة عبر Cookie.
- **مثال:** `AuthRemoteDataSource.refresh` يرسل جسماً فارغاً مع `attachCsrf`.
- **تحقق:** اختبار CSRF interceptor + session restore بدون cookie → unauthenticated.

## حوكمة الإغلاق

أُغلق بموجب أمر المالك **Autonomous Sequential Web Development** (تفويض مسبق مشروط بتحقق القبول)، بما يوازي استثناء الموبايل التسلسلي. بنود الخادم الحي المؤجلة لا تُحتسب PASSED.
