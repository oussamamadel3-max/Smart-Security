# تقرير WEB-C-01 — Architecture & Code Quality Audit

| الحقل | القيمة |
|--------|--------|
| المهمة | `WEB-C-01` |
| الحالة | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` فقط. الحزمة المشتركة لم تُمس. لا تعديل على `security_mobile` / Backend / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` خارج النطاق. لا اعتماد عقد جديد |
| العدّاد | لا تغيير على 8/8 ولا على 21/50 |

## 1. النطاق والاعتماديات

تدقيق طبقات Presentation → Domain ← Data، وRiverpod، وGoRouter، وDio، ومسار الأخطاء، والمؤقتات/`dispose`، وتكرار النماذج مقابل `smart_security_app_core`، والملفات الميتة، وMocks الإنتاج.

الاعتماد: بطاقات `WEB-T-01`…`WEB-T-08` مغلقة ضمن نطاق العميل. لا حاجة لاعتماد G1 لهذا التدقيق البرمجي.

## 2. الملفات

| ملف | التغيير |
|------|---------|
| `admin_web/lib/features/owner/presentation/pages/owner_home_placeholder_page.dart` | حُذف. غير مستورد. الصفحة الرئيسية للمالك هي `DevicesListPage` على `AppRoutes.ownerHome` |
| `admin_web/lib/features/devices/domain/usecases/create_device_command_usecase.dart` | جديد. إنشاء الأمر يمر عبر UseCase |
| `admin_web/lib/features/devices/presentation/providers/devices_providers.dart` | `createDeviceCommandUseCaseProvider` |
| `admin_web/lib/features/devices/presentation/viewmodels/device_detail_view_model.dart` | إيقاف الاستعلام بجيل، و`pausePolling` / `resumePollingIfNeeded`، واستعلام GET عبر `GetDeviceCommandUseCase` |
| `admin_web/lib/features/devices/presentation/pages/device_detail_page.dart` | إيقاف المؤقت عند `dispose` / `didPushNext` / `hidden` / `paused`، واستئنافه عند العودة |
| `admin_web/lib/core/network/web_network_providers.dart` | `ref.onDispose(() => dio.close())` لـ `webDioProvider` |
| `admin_web/lib/core/network/auth_dependency_providers.dart` | الإغلاق نفسه لـ `bareDioProvider` |
| `admin_web/test/features/devices/device_detail_command_guards_test.dart` | اختبار إيقاف/استئناف GET |
| `admin_web/pubspec.yaml` · `pubspec.lock` | `fake_async: ^1.3.3` كتبعية تطوير مباشرة حتى يرضى `depend_on_referenced_packages` |

لا أثر على مخطط قاعدة البيانات، ولا على عقود MQTT/HTTP، ولا على عزل الملكية، ولا على مكان حفظ Access (يبقى في الذاكرة). إغلاق `Dio` يحدث عند التخلص من المزود فقط.

## 3. النتائج

| الشدة | المنطقة | النتيجة | الحكم |
|--------|---------|---------|--------|
| — | حرج | لا خرق حرج في الطبقات أو في مسار المصادقة | لا إجراء |
| عالية | مؤقت الأوامر | `DeviceDetailViewModel` يستخدم `NotifierProvider.family` بلا `autoDispose`، فـ `Timer.periodic` كان يستمر حتى ~60 ثانية بعد مغادرة التفاصيل | **أُصلح**. لم يُحوَّل المزود إلى autoDispose حتى لا يسقط قيد `TIMED_OUT` عند فتح السجل والعودة |
| متوسطة | الطبقات | `submitCommand` واستعلام الحالة كانا يناديان `DeviceCommandsRepository` مباشرة رغم وجود UseCases للقراءة والقائمة | **أُصلح** عبر `CreateDeviceCommandUseCase` و`GetDeviceCommandUseCase` |
| متوسطة | جلسة متأخرة | `DevicesListViewModel.load` (ومسارات تحميل مشابهة) قد تكتب نجاحاً قديماً بعد تسجيل الخروج لأن المزود ليس autoDispose | **مؤجّل إلى `WEB-C-02`** (عزل الذاكرة وrace/late). لم يُمس هنا |
| منخفضة | Dio | العميل لم يكن يُغلق عند التخلص من المزود | **أُصلح** |
| منخفضة | كود ميت | `OwnerHomePlaceholderPage` | **حُذف** |
| منخفضة | تركيب DI | مزودات العرض تستورد تنفيذ Data. هذا جذر التركيب الحالي لكل الميزات | مقبول. نقلها إعادة هيكلة واسعة بلا عيب سلوكي |
| منخفضة | Domain | `RegisterDeviceRules` يستخدم `dart:convert` للتحقق أن قاعدة الإنذار كائن JSON | مقبول. ليس DTO ولا تسريباً لطبقة Data |
| منخفضة | GoRouter | `appRouterProvider` لا يستدعي `GoRouter.dispose` | مقبول. المزود عمره عمر العملية؛ التخلص المبكر يتسابق مع `MaterialApp.router` |
| معلومة | تعدادات الحالة | المappers تستخدم `OwnershipStatus.parse` و`ArmStatus` و`CommandStatus` و`AlarmEventStatus` من الحزمة. لا تعداد محلي مكرر | لا توحيد مطلوب |
| معلومة | `security_mobile` | لا استيراد | — |
| معلومة | Mock إنتاجي | المزيفات داخل `test/` فقط (`SharedPreferences.setMockInitialValues` في الاختبار) | — |
| معلومة | الحالة | Riverpod فقط. لا `flutter_bloc` | — |
| معلومة | التوجيه | `authGuardRedirect` هو قرار إعادة التوجيه للمصادقة. `context.go` في الصفحات تنقّل صريح وليس التقاط 401 | — |
| معلومة | الأخطاء | `Result` → `FailureHandler` → `ErrorState`. `prettyLogInterceptor` تحت `kDebugMode` ومن دون أجسام | — |
| معلومة | النصوص | نسخ عربية ثابتة في `AdminCopy` ورسائل الأوامر وليست كلها في ARB | مؤجّل إلى تدقيق الواجهة `WEB-C-03`…`WEB-C-05` |
| معلومة | `clearCache` | الـ ViewModel يستدعيه عند 403/فقدان الجلسة، والمستمع على المزود يفعل الشيء نفسه | مقبول كأثر جانبي لعقد المستودع |

`TextEditingController` و`TabController` ومراقب المسار تُلغى في `dispose`. لا تسريب إضافي ظاهر من هذا النوع.

## 4. مصفوفة القبول

| البند | الحكم | الدليل |
|--------|--------|--------|
| اتجاه الطبقات | VERIFIED | Domain لا يستورد Flutter/Dio. إنشاء الأمر واستعلامه يمران بـ UseCase. بقايا DI مذكورة أعلاه |
| Riverpod / GoRouter / Dio | VERIFIED | قراءة `app_router.dart` و`auth_guard.dart` و`web_network_providers.dart` و`auth_interceptor.dart` و`DioClientSetup` |
| مسار الأخطاء | VERIFIED | لا `Dio()` محلي في الميزات. الفشل يعبر `Result` |
| لا تكرار جوهري مع الحزمة | VERIFIED | تعدادات الحالة من `device_status_codes.dart` |
| لا كود ميت معروف | VERIFIED | حذف placeholder. لا مرجع متبقٍ |
| لا Mock إنتاجي | VERIFIED | بحث في `lib/` |
| مؤقت الاستعلام | VERIFIED | `pausePolling stops further command status GETs` |
| analyze / test / build | VERIFIED | الأوامر أدناه |
| سباق نجاح متأخر بعد الخروج | PARTIAL | مؤجّل إلى `WEB-C-02` |
| CSRF/CORS حي، G1/ACK، متصفح ضد Backend | NOT VERIFIED | خارج نطاق هذا التدقيق البرمجي |
| الحزمة و`security_mobile` | NOT VERIFIED كإعادة تشغيل | لم يتغير الكود هناك؛ لا أمر analyze عليهما |

## 5. الأوامر

من `admin_web`:

| الأمر | النتيجة |
|--------|---------|
| `flutter pub get` | نجح. `fake_async` 1.3.3 أصبحت `direct dev` |
| `flutter analyze` | **No issues found** (47.8s) |
| `flutter test` | **81/81** نجحت (الاختبار الجديد ضمن `device_detail_command_guards_test.dart`) |
| `flutter build web --release` | **exit 0**. `√ Built build\web` (453.3s). تحذير Wasm dry run فقط |

لم يُشغَّل analyze للحزمة ولا للموبايل لأن الحزمة لم تتغير.

## 6. المخاطر المتبقية

- نجاح قائمة الأجهزة المتأخر بعد تسجيل الخروج ما زال ممكناً في الذاكرة. بطاقة `WEB-C-02`.
- إيقاف الاستعلام يبقي آخر `pendingCommand` في المزود الحي. هذا مقصود حتى لا يختفي حظر `TIMED_OUT` عند زيارة السجل.
- لا ادّعاء بتجربة متصفح حي ولا بإغلاق G1.

## 7. الحكم

**CLOSED** لنطاق البرمجيات في `WEB-C-01`. التالية: `WEB-C-02`.

## 🧠 مقتطف هندسي من المهمة

`Timer.periodic` داخل `DeviceDetailViewModel` لا يموت بمغادرة الصفحة عندما يكون المزود `NotifierProvider.family` من غير `autoDispose`: Riverpod يبقي الـ Notifier، فيستمر GET كل ثانيتين حتى سقف `CommandPollPolicy` (حوالي 60 ثانية). تحويل المزود إلى autoDispose كان سيلغي المؤقت لكنه يسقط أيضاً قيد `TIMED_OUT` المخزّن في الحالة عند فتح السجل والعودة، لأن `load` يعيد الجهاز فقط ولا يسترجع الأمر المعلّق. الحل هو إيقاف المؤقت من دورة حياة المسار (`dispose` و`didPushNext` والإخفاء) مع عدّاد `_pollGeneration` يُزاد في `_stopPolling` فيتجاهل الطلب الطائر أي كتابة حالة بعد الإيقاف، ثم `resumePollingIfNeeded` يكمل الميزانية المتبقية. يُستخدم هذا عندما يكون للمؤقت أثر شبكي ويجب أن تبقى حالة الشاشة بعد التنقل. التحقق العملي: اختبار `pausePolling stops further command status GETs` يثبت أن عداد `getById` لا يزيد خلال 6 ثوانٍ بعد الإيقاف ثم يزيد بعد الاستئناف.
