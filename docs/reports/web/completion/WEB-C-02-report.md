# تقرير WEB-C-02 — Authentication, Session & Authorization Audit

| الحقل | القيمة |
|--------|--------|
| المهمة | `WEB-C-02` |
| الحالة | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` فقط. الحزمة المشتركة لم تُمس. لا تعديل على Backend / mobile / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` خارج النطاق |
| العدّاد | لا تغيير على 8/8 ولا على 21/50 |

## 1. النطاق والاعتماديات

تدقيق Access في الذاكرة، وCookie/CSRF، وsingle-flight، و401 مقابل 403، واستعادة الجلسة، والاستجابات المتأخرة بعد الخروج، وعزل الذاكرة المؤقتة، وحراسة `OWNER`/`ADMIN`، وعدم معاملة claims الـ JWT كتفويض مستقل، وعدم تسجيل الأسرار.

الاعتماد: `WEB-C-01` مغلقة. السباق المؤجّل منها (نجاح قائمة الأجهزة بعد الخروج) تأكد أنه حقيقي وأُصلح هنا.

## 2. الملفات

| ملف | التغيير |
|------|---------|
| `admin_web/lib/core/session/session_epoch.dart` | جديد. `SessionEpoch` و`sessionResultApplies` |
| `admin_web/lib/core/session/auth_session_controller.dart` | إبطال العصر عند المسح/انتهاء الجلسة/تبديل المستخدم. `markSessionExpired` لا يحيي خروجاً مكتملاً ولا يكرر الرفع |
| `admin_web/lib/core/network/auth_interceptor.dart` | لا يعيد إرسال كتابة غير آمنة بعد التجديد إلا إن وُجد `idempotencyKey` |
| `admin_web/lib/core/router/auth_guard.dart` | توثيق أن فصل الدور تجربة مستخدم والخادم هو سلطة التفويض |
| `admin_web/lib/features/devices/presentation/viewmodels/devices_list_view_model.dart` | نجاح متأخر بعد الخروج لا يكتب القائمة |
| `admin_web/lib/features/devices/presentation/viewmodels/device_detail_view_model.dart` | إيقاف الاستعلام ومسح الحالة عند فقد الجلسة، وتجاهل النجاح المتأخر |
| `admin_web/lib/features/devices/data/repositories/devices_repository_impl.dart` | لا يملأ الكاش إن تغيّر العصر أثناء الطلب |
| `admin_web/lib/features/devices/data/repositories/device_commands_repository_impl.dart` | الحارس نفسه على كاش الأوامر |
| `admin_web/lib/features/devices/data/repositories/device_alarm_events_repository_impl.dart` | الحارس نفسه على كاش الحوادث |
| `admin_web/lib/features/devices/presentation/providers/devices_providers.dart` | عصر جلسة مشترك |
| `admin_web/lib/features/admin/data/repositories/admin_devices_repository_impl.dart` | لا يعيد ملء كاش الأدمن ولا يمسحه بكتابة متأخرة |
| `admin_web/lib/features/admin/data/repositories/owner_accounts_repository_impl.dart` | الحارس نفسه لحسابات المالك |
| `admin_web/lib/features/admin/presentation/providers/admin_providers.dart` | عصر جلسة مشترك |
| `admin_web/lib/features/admin/presentation/viewmodels/admin_devices_view_model.dart` | تذكرة جلسة تمنع تطبيق قائمة متأخرة |
| `admin_web/lib/features/admin/presentation/viewmodels/owner_accounts_view_model.dart` | التذكرة نفسها |
| `admin_web/test/features/devices/session_late_response_test.dart` | جديد |
| `admin_web/test/features/auth/auth_repository_impl_test.dart` | جديد |
| اختبارات قائمة/جلسة/حارس/اعتراض موجودة | حالات عدم التكرار، وانتهاء الجلسة، والكاش، و`sessionExpired` |

لا أثر على مخطط قاعدة البيانات ولا على عقود MQTT/HTTP. مكان Access يبقى الذاكرة. Refresh يبقى Cookie من المتصفح.

## 3. النتائج

| الشدة | المنطقة | النتيجة | الحكم |
|--------|---------|---------|--------|
| عالية | قائمة الأجهزة | `DevicesListViewModel.load` كان يكتب `onSuccess` بعد أن يصفّر الخروج الحالة، لأن المزود ليس `autoDispose` | **أُصلح**. الاختبار `late device list success after logout does not restore rows` |
| عالية | تفاصيل الجهاز | المزود يبقى حياً، و`load` واستعلام الأمر كانا يكتبان بعد الخروج | **أُصلح**. المستمع يوقف المؤقت ويصفّر الحالة. الاختبار `late device detail success after logout does not restore the device` |
| متوسطة | الكاش | الكتابة داخل المستودع كانت تتم بعد `clearCache` إذا عاد الطلب متأخراً | **أُصلح** عبر `SessionEpoch`. الاختبار `late list success after session epoch invalidate does not refill cache` |
| متوسطة | إعادة الإرسال | `AuthInterceptor` كان يعيد أي طريقة بعد 401، بما فيها POST بلا مفتاح عدم تكرار | **أُصلح**. GET يُعاد. POST بلا مفتاح لا يُعاد. POST مع `idempotencyKey` يُعاد مرة واحدة |
| منخفضة | انتهاء الجلسة | `markSessionExpired` بعد خروج مكتمل كان يبدّل الحالة إلى `sessionExpired`، والتكرار كان يرفع الجيل | **أُصلح** |
| — | Access | `AccessTokenHolder` في الذاكرة فقط. `SharedPreferences` للموقع اللغوي في الحزمة. لا `localStorage` / `sessionStorage` / `IndexedDB` للرموز | VERIFIED بالقراءة |
| — | Cookie / CSRF | `BrowserHttpClientAdapter.withCredentials = true`. الترويسة `X-CSRF-TOKEN` على التجديد والخروج عند علم `attachCsrf`. اسم الكوكي `esp32_csrf` غير HttpOnly، و`esp32_rt` لا يُقرأ من JS | VERIFIED في الكود. الحي **NOT VERIFIED** |
| — | single-flight / 401 / 403 | تجديد واحد للطلبات المتزامنة. 403 لا يجدّد ولا ينهي الجلسة. `skipAuthRefresh` يمنع حلقة التجديد | VERIFIED باختبارات سابقة ما زالت خضراء |
| — | logout-all | `AuthController.LogoutAll` يعتمد Bearer ولا يستدعي فحص CSRF. العميل يرسل Bearer ولا يرفق CSRF. متوافق مع العقد الحالي | VERIFIED بقراءة المتحكم. لا تغيير |
| — | الحراسة | `authGuardRedirect` يمنع تلاعب المسار بين المالك والأدمن ويرسل `sessionExpired` إلى الدخول | VERIFIED |
| — | JWT | أدوار الواجهة تُقرأ من حمولة Access لأن `MeResponseDto` لا يحمل أدواراً. الحراسة تجربة مستخدم. 403 لا يسجّل خروجاً | VERIFIED كتصميم. ليست إثباتاً مستقلاً للتفويض |
| — | السجلات | `prettyLogInterceptor` يطفئ أجسام الطلب والاستجابة. لا طباعة لكلمة المرور أو الرمز | VERIFIED بالقراءة |
| معلومة | نماذج AutoDispose | تفاصيل الأدمن ونماذج الكتابة تُتلف مع مغادرة المسار. وميض نجاح قبل `dispose` لا يبقى للمستخدم التالي | مقبول. قوائم الأدمن والحسابات صارت تفحص الجيل |

## 4. مصفوفة القبول

| البند | الحكم | الدليل |
|--------|--------|--------|
| Access في الذاكرة فقط | VERIFIED | `AccessTokenHolder` و`login stores access in memory only` |
| Cookie + CSRF وفق العقد | VERIFIED في الكود | `web_credentials_adapter_web.dart` و`CsrfHeaderInterceptor` و`AuthRemoteDataSource.refresh/logout` |
| Cookie حي ضد Backend | NOT VERIFIED | لا متصفح ولا Origin حي في هذه الجولة |
| single-flight ومنع حلقة التجديد | VERIFIED | `single-flight refresh for concurrent 401s` و`skipAuthRefresh prevents refresh loop` |
| 401 مقابل 403 | VERIFIED | 403 لا يجدّد. فشل التجديد يستدعي `onSessionExpired` مرة واحدة |
| خروج / خروج الكل / استعادة | VERIFIED برمجياً | `logout` و`logoutAll` و`restore without csrf cookie stays unauthenticated` |
| استجابة متأخرة بعد الخروج | VERIFIED | اختبارا القائمة والتفاصيل، واختبار الكاش، و`late refresh after logout does not restore session` |
| عزل الكاش | VERIFIED | `SessionEpoch` على قوائم المالك والأوامر والحوادث والأدمن والحسابات |
| لا إعادة كتابة غير آمنة بلا مفتاح | VERIFIED | `POST without idempotency is not replayed` و`POST with idempotencyKey is replayed once` و`POST create is not auto-retried` |
| حراسة المسارات | VERIFIED | `auth_guard_test.dart` بما فيه `sessionExpired forces login` |
| JWT ليس تفويضاً مستقلاً | VERIFIED | تعليق الحارس + 403 لا ينهي الجلسة |
| استجابات مشوّهة | VERIFIED | `auth_repository_impl_test.dart` لرمز فارغ وrefresh في جسم قناة Cookie وقناة غير Cookie |
| analyze / test / build | VERIFIED | الأوامر أدناه |
| G0 / G1 / G2 وCSRF الحي | NOT VERIFIED | خارج النطاق |

## 5. الأوامر

من `admin_web`:

| الأمر | النتيجة |
|--------|---------|
| `flutter pub get` | نجح. لا تغيير على القيود |
| `flutter analyze` | **No issues found** (27.2s) |
| `flutter test` | **92/92** نجحت |
| `flutter build web --release` | **exit 0**. `√ Built build\web` (276.6s). تحذير Wasm dry run فقط |

لم يُشغَّل analyze للحزمة لأن الحزمة لم تتغير.

## 6. المخاطر المتبقية

- الكوكي الحي وCSRF/Origin ضد Backend حقيقي **NOT VERIFIED**.
- نماذج الكتابة ذات `autoDispose` قد تُظهر نجاحاً لحظةً قبل إتلاف الصفحة. الحالة لا تنتقل إلى الجلسة التالية.
- أدوار التوجيه ما زالت من حمولة JWT غير الموقّعة محلياً لأن عقد `/me` لا يعيد الأدوار. الخادم يبقى سلطة القبول.
- لا ادّعاء بإغلاق G1 أو بتجربة متصفح.

## 7. الحكم

**CLOSED** لنطاق البرمجيات في `WEB-C-02`. التالية: `WEB-C-03`.

## 🧠 مقتطف هندسي من المهمة

الخروج يرفع `AuthSessionState.generation` ويبطل `SessionEpoch` داخل `_hardClear` قبل أن يعود الطلب الطائر. `DevicesListViewModel.load` يلتقط الجيل قبل `await`، وبعد العودة يرفض الكتابة إن فشل `sessionResultApplies`. المستودع يلتقط العصر نفسه ويرفض `_listCache` إن استدعى `invalidate` أثناء الانتظار. بدون الفحصين كان المستمع يصفّر القائمة ثم يكتب `onSuccess` أجهزة المالك السابق لأن المزود ليس `autoDispose`. يُستخدم هذا لكل قراءة تملأ واجهة أو ذاكرة وتستطيع أن تكتمل بعد انتهاء الجلسة. التحقق: `late device list success after logout does not restore rows` يثبت أن القائمة تبقى فارغة، و`late list success after session epoch invalidate does not refill cache` يثبت أن `hasCachedList` يبقى false رغم نجاح النتيجة.
