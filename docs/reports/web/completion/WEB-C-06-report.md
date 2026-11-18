# تقرير WEB-C-06 — Final Software Verification & Documentation

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | `WEB-C-06` |
| الاسم | Final Software Verification & Documentation |
| الحالة النهائية | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` وتقارير الويب فقط. الحزمة المشتركة لم تُمس. لا تعديل على Backend / mobile / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` لم تُغلق |
| العدّاد | 8/8 و21/50 بلا تغيير |
| الحكم | **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING** |
| التالية | لا مهمة `WEB-C-*` ولا مهمة `WEB-T-*`. الانتظار لتوجيه المالك. ليس مشروعًا جديدًا |

## 3. الملخص التنفيذي

أُعيد تشغيل التحقق الكامل داخل `admin_web` في 2026-11-18: `flutter pub get` و`flutter analyze` و`flutter test` (**121/121**) و`flutter build web --release` (exit 0). لم تُضف اختبارات جديدة؛ مراجعة القبول الحرج وجدت التغطية قائمة من `WEB-T-08` ومن `WEB-C-01`…`WEB-C-05`. قُدّمت `build/web` محليًا عبر `HttpListener` لأن Python غير متاح، وظهرت شاشة الدخول من النسخة المبنية. قورنت مع `design/index.html` لشاشة الدخول فقط. `WEB-EXT-09` اكتمل بهذا الدليل. `WEB-EXT-08` يبقى مفتوحًا لأن بقية الشاشات لم تُعايَن من النسخة المبنية. لا جاهزية إنتاج.

## 4. نطاق المراجعة

- تقارير `WEB-C-01`…`WEB-C-05` وأحكامها.
- قائمة `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md` ودخان `web_acceptance_smoke_test.dart`.
- عيّنة المسارات الحرجة: Access في الذاكرة، single-flight، ترويسة `X-CSRF-TOKEN`، 403 لا يجدّد الجلسة، `PENDING` ليس تنفيذًا، شاشة جهاز الأدمن بلا أوامر مالك، نجاح متأخر بعد الخروج.
- `lib/` بحثًا عن أسرار وبيانات النموذج التجريبي.
- `AppEnvironment` و`main.dart.js` بعد البناء.
- محاولة `WEB-EXT-08` و`WEB-EXT-09` بلا خدمات خارجية.

## 5. المشاكل المكتشفة

لا عيب برمجي جديد ظهر في هذه الجولة يستدعي تعديل `lib/`. الفجوات المعروفة سُجّلت سابقًا وما زالت:

| الشدة | الموضوع | المصدر | في هذه الجولة |
|--------|---------|--------|----------------|
| — | لا فشل analyze / test / build | تشغيل 2026-11-18 | لم يُكسر شيء |
| خارج النطاق | CSRF/Origin الحي، Reload بالكوكيز، تعطيل حساب، G1/ACK، نقل ملكية حي، FCM، متصفح ضد Backend | `WEB-EXT-01`…`07` | بقيت مفتوحة |
| بصرية | شريط الأدمن العريض، درج 548، تفاصيل المالك والسجل بلا لقطة متصفح، صف السجل الممتلئ عند ≥900 بلا ضخ معرّف طويل | `WEB-C-03`…`C-05` | لم تُغلق. الدخول من `build/web` قورن فقط |
| معلومة | البناء الافتراضي يضم `APP_ENV=dev` وعنوان `http://localhost:5080` | `app_environment.dart` و`main.dart.js` | موثّق، بلا ادّعاء بناء إنتاج |

## 6. تصنيف الخطورة

لا بند عالي جديد داخل برنامج الويب القابل للإصلاح الآن. البنود العالية المتبقية اعتمادات خارجية (حي/بوابات) وليست عيوب عميل ظهر أن الاختبارات الحالية تفشل فيها. الفجوات البصرية متوسطة أو منخفضة ولا تمنع حكم الاكتمال البرمجي، وتبقى موثّقة في `WEB-EXT-08`.

## 7. التحسينات المنفذة

لا تعديل سلوكي في هذه المهمة. العمل توثيق وتحقق وإغلاق `WEB-EXT-09`. دروس `WEB-T-06` و`WEB-T-07` و`WEB-T-08` أُضيفت في 2026-11-18 دون اختلاق تشغيل تاريخي.

## 8. الملفات المضافة

- `docs/reports/web/completion/WEB-C-06-report.md`
- `docs/reports/web/completion/WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md`
- `docs/reports/web/evidence/release/release-login.png`
- `docs/reports/web/evidence/release/design-login.png`

## 9. الملفات المعدلة

- `admin_web/WEB-REMAINING-EXTERNAL-WORK.md`
- `admin_web/web-completion-tasks.md`
- `admin_web/web-current.md`
- `admin_web/web-progress.md`
- `admin_web/web-context.md`
- `docs/reports/web/WEB-FINAL-REPORT.md`
- `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`
- `docs/reports/web/WEB-T-06-report.md`
- `docs/reports/web/WEB-T-07-report.md`
- `docs/reports/web/WEB-T-08-report.md`
- `system-context.md`
- `system-progress.md`

## 10. الملفات المحذوفة

لا حذف.

## 11. القرارات التقنية

- لا اختبار جديد. القبول الحرج مغطى، وصف السجل عند ≥900 يبقى `PARTIAL` كما في `WEB-C-05` لأن القص موجود في `_DataRow` ولم يُضخ معرّف طويل. ذلك ليس ثقبًا وظيفيًا.
- إغلاق `WEB-EXT-09` بعد تحميل الصفحة من `build/web`. Python غير موجود؛ الخادم كان `System.Net.HttpListener` على `http://127.0.0.1:8099/`.
- إبقاء `WEB-EXT-08` مفتوحًا. لقطة الدخول لا تغطي الشاشات بعد المصادقة، ومحاكاة العرض 1440 في لوحة المتصفح المضمّنة قصّت زاوية البطاقة فلم تُعتمد كدليل للشريط العريض.
- البناء المعتمد هو `flutter build web --release` بلا `--dart-define`. الناتج ليس تهيئة إنتاج.
- لا رفع لـ 8/8 ولا 21/50. لا `PRODUCTION READY`.

## 12. الاختبارات المضافة

لا ملف اختبار جديد في `WEB-C-06`.

العيّنة التي أعادتها مجموعة **121/121** في هذا التشغيل (ليست أعدادًا تاريخية منفصلة):

| الاختبار | الملف |
|----------|--------|
| `login stores access in memory only` | `auth_session_controller_test.dart` |
| `single-flight refresh for concurrent 401s` | `auth_interceptor_test.dart` |
| `403 does not refresh or expire session` | `auth_interceptor_test.dart` |
| `attaches CSRF header when extra flag is set` | `csrf_header_interceptor_test.dart` |
| `command statuses do not treat HTTP accept as device execution` | `web_acceptance_smoke_test.dart` |
| `admin device screen shows PENDING and no owner commands` | `admin_device_detail_page_test.dart` |
| `late device list success after logout does not restore rows` | `session_late_response_test.dart` |
| `logout drops in-memory access and history cache` | `web_acceptance_smoke_test.dart` |

لقطة `WEB-T-08` الأصلية تبقى **80/80** في 2026-11-18 قبل مرحلة الاستكمال. الفرق تراكمي داخل المجموعة نفسها، وليس مجموعًا يُضاف إلى 121.

## 13. نتائج Flutter Analyze

من `admin_web` في 2026-11-18 لهذه المهمة: **No issues found** (110.9s)، exit 0.

## 14. نتائج Flutter Test

`flutter test` في `admin_web`: **121/121** نجحت، exit 0. عدّاد النهاية `00:59 +121: All tests passed!`. لا تخطّي ظاهر في المخرج.

## 15. نتائج Flutter Build Web

`flutter build web --release` من `admin_web`: exit 0. `√ Built build\web`. تجميع `lib\main.dart` ‏189.0s. المدة الكلية للعملية حوالي 210.7s. تحذير Wasm dry run فقط؛ لم يُبنَ `--wasm`. لا `--base-href` ولا `--dart-define` إضافي. البحث في `main.dart.js` أظهر العناوين الافتراضية فقط: `http://localhost:5080` و`https://staging-api.example.invalid` و`https://api.example.invalid`. لم يظهر `ahmad@example` ولا مفتاح خاص ولا `AIza`.

## 16. جدول تحقق شروط القبول

| الشرط | الحكم | الدليل |
|--------|--------|--------|
| مراجعة `WEB-C-01`…`C-05` | PASSED | التقارير مقروءة؛ لا تعارض مع هذا التشغيل |
| مراجعة قبول `WEB-T-01`…`T-08` والقائمة | PASSED للمراجعة | الأحكام التاريخية أُبقيت. التشغيل الجديد 121/121 |
| لا كسر للوظائف السابقة | PASSED | المجموعة كاملة خضراء |
| لا بيانات تجريبية في مسار الإنتاج | PASSED | `main.dart` يشغّل `HisnWebApp`. معاينات `tool/*_visual_preview.dart` خارج الموجّه. الدخول المبني فارغ |
| لا أسرار في الكود أو الناتج | PASSED للبحث المنفَّذ | القسم 15 |
| بيئات dev/staging/prod بلا نشر | PASSED كقراءة | `app_environment.dart`. لم يُنشر شيء |
| analyze / test / build | PASSED | الأقسام 13–15 |
| تقديم `build/web` محليًا | PASSED | `WEB-EXT-09`. اللقطة `evidence/release/release-login.png` |
| مطابقة بصرية كاملة | PARTIAL | دخول النسخة المبنية مقابل التصميم فقط. `WEB-EXT-08` مفتوح |
| G0 / G1 / G2 والحي | NOT VERIFIED | خارج النطاق |
| جاهزية إنتاج | ليست متحققة | لا يُستخدم حكم `PRODUCTION READY` |

## 17. الأدلة البصرية

| اللقطة | ماذا تثبته | ماذا لا تثبته |
|--------|------------|----------------|
| `docs/reports/web/evidence/release/release-login.png` | دخول RTL من `http://127.0.0.1:8099/` بعد بناء release. الحقول فارغة. العنوان «مرحباً بعودتك». زر «تسجيل الدخول». لا شريط «انتهت الجلسة» | ليست جلسة، ولا Backend، ولا تطابق بكسل |
| `docs/reports/web/evidence/release/design-login.png` | نموذج `design/index.html` على `http://127.0.0.1:8100/` بعد الضغط على «تسجيل الدخول» | بيانات `ahmad@example.test` وزر «تسجيل الدخول (تجريبي)» وشريط أدوار. هذا النموذج لا يُنقل |
| محاولة عرض 1440 | قصّت زاوية البطاقة داخل لوحة المتصفح | **NOT VERIFIED** كتخطيط عريض للنسخة المبنية |

فروقات الدخول المقصودة: حقل الإنتاج «اسم المستخدم» فارغ؛ التصميم «البريد الإلكتروني» مع قيمة تجريبية. الإنتاج بلا شريط جانبي ولا مبدّل مالك/إدارة/هاتف. سطر أسفل البطاقة يذكر أن Access في الذاكرة والبيئة `dev` وقناة Refresh هي Cookie. لا ادّعاء تطابق بكسل.

لقطات `evidence/owner/` و`evidence/admin/` تبقى أدلة `WEB-C-03` و`WEB-C-04` بتاريخ 2026-11-18، ومعظمها من معاينات `tool/` عند 548×594 لا من `build/web`.

## 18. الأعمال المتبقية

`WEB-EXT-01`…`WEB-EXT-08` مفتوحة. `WEB-EXT-09` مكتمل 2026-11-18. التفاصيل في `admin_web/WEB-REMAINING-EXTERNAL-WORK.md`.

## 19. الاعتماديات الخارجية

Backend حي، Cookie/CSRF/Origin، تعطيل حساب، اعتماد G1 وجهاز أو محاكاة بروتوكول لـ ACK، نقل ملكية على خادم متصل، FCM حي، ومتصفح تفاعلي ضد API الفعلي. لا شيء من ذلك شُغّل هنا.

## 20. حالة المهمة التالية

لا تالية داخل الاستكمال ولا داخل `WEB-T-*`. المرحلة البرمجية للويب تقف بانتظار توجيه المالك نحو الاعتمادات الخارجية أو مشروع آخر. لم يُبدأ مشروع جديد.

## الدرس الهندسي المستفاد

المفهوم: ثوابت `String.fromEnvironment` تُخبَز عند الترجمة ولا تُقرأ من ملف عند تشغيل المتصفح.

كيف يعمل: `AppEnvironment.current` يقرأ `APP_ENV` بقيمة افتراضية `dev`، و`apiBaseUrl` يقرأ `API_BASE_URL` أو يستخدم `http://localhost:5080` في التطوير و`https://api.example.invalid` في الإنتاج. `flutter build web --release` بلا `--dart-define` يُنتج الفرع `dev`.

لماذا: حتى لا يُؤخذ نجاح البناء على أنه تهيئة إنتاج، ولا يُخترع عنوان خادم.

متى يُستخدم في مشاريع أخرى: أي عميل يفرّق البيئات عند الترجمة بدل ملف أسرار في المستودع.

مثال من التنفيذ: `admin_web/lib/core/config/app_environment.dart`، وفي `admin_web/build/web/main.dart.js` ظهرت السلاسل الثلاث بعد هذا البناء.

التحقق: ابحث في `main.dart.js` عن `localhost:5080` بعد بناء بلا `--dart-define`. بناء الإنتاج المقصود يحتاج `APP_ENV=prod` و`API_BASE_URL` الحقيقيين، وهذا لم يُنفَّذ هنا ولم يُنشر.
