# تقرير FCM-C-03 — ربط Firebase SDK بتطبيق Flutter واستبدال NoOpFcmMessaging

| الحقل | القيمة |
|--------|--------|
| المهمة | **FCM-C-03** |
| التاريخ | 2026-11-19 |
| الحالة | **SOFTWARE COMPLETE — BUILD VERIFICATION PENDING** |
| النطاق | `security_mobile` فقط · Android flavors (dev/staging/prod) · لا iOS |
| لم يُنفَّذ | بناء APK فعلي ناجح لأي flavor (شبكة المالك ضعيفة أثناء الجلسة — مؤجَّل بطلبه) · تشغيل على جهاز/محاكٍ (ممنوع) |

مراجع: `docs/fcm/fcm-completion-tasks.md` · `docs/fcm/FCM-ENVIRONMENTS.md` · إغلاق FCM-C-01/C-02 · `MOB-T-06`

---

## 1) الهدف والنطاق

تفعيل عميل Firebase Cloud Messaging الحقيقي في تطبيق Flutter بدل `NoOpFcmMessaging`، مع الحفاظ على عقد `FcmMessagingPort` الحالي (`security_mobile/lib/core/push/fcm_messaging_port.dart`) كما هو، وربط Google Services Gradle Plugin بكل flavor (dev/staging/prod) دون تفعيل إرسال Backend حي أو لمس Flutter/iOS خارج النطاق.

## 2) الحالة قبل التنفيذ (مُتحقَّقة من الكود، لا من التقارير فقط)

| عنصر | الحالة الفعلية قبل C-03 |
|------|--------------------------|
| `FcmMessagingPort` | موجود، مع `NoOpFcmMessaging` كتنفيذ وحيد |
| `fcmMessagingPortProvider` | يُرجع `const NoOpFcmMessaging()` دائماً |
| `pubspec.yaml` | بلا `firebase_core`/`firebase_messaging` |
| `android/app/build.gradle.kts` | بلا google-services plugin |
| `google-services.json` | موجودة في `src/dev`, `src/staging`, `src/prod` (من FCM-C-01) لكن غير مُستخدَمة فعلياً |
| `PushTokenLifecycle` / `NotificationOpenCoordinator` | منفَّذان بالكامل فوق `FcmMessagingPort` (MOB-T-06) — لا حاجة لإعادة بنائهما |

## 3) الفجوات المكتشفة

1. لا يوجد تنفيذ حقيقي للمنفذ — `requestPermission` يُرجع `false` دائماً، `getToken` يُرجع `null` دائماً.
2. Gradle لا يقرأ `google-services.json` لأن الإضافة غير مُفعَّلة.
3. `AndroidManifest.xml` الرئيسي لا يحتوي `POST_NOTIFICATIONS` (مطلوب Android 13+ لعرض الإذن).
4. `firebase_messaging` الإصدار 17.x يُهمِل (`@Deprecated`) الثنائي `getToken()`/`onTokenRefresh` لصالح `register()`/`onRegistered` — وهذا الأخير يُرجع **Firebase Installation ID (FID)** لا توكن FCM التقليدي الذي بُني عليه عقد `/api/me/push-tokens` و`FirebaseFcmProvider.SendAsync` في BE-T-09.

## 4) الأعمال المنفَّذة

### 4.1 الحزم

- `flutter pub add firebase_core firebase_messaging` → حُلَّت تلقائياً: `firebase_core ^5.1.0`، `firebase_messaging ^17.0.1` (+ التبعيات الانتقالية: `_flutterfire_internals`, `firebase_core_platform_interface`, `firebase_core_web`, `firebase_messaging_platform_interface`, `firebase_messaging_web`).

### 4.2 Gradle

- `android/settings.gradle.kts`: إضافة `id("com.google.gms.google-services") version "4.4.2" apply false`.
- `android/app/build.gradle.kts`: تطبيق `id("com.google.gms.google-services")` بعد Flutter Gradle Plugin — يقرأ `google-services.json` الخاص بكل flavor تلقائياً من `src/<flavor>/` دون أي تغيير في أسماء الـ flavors أو applicationIdSuffix الحالية.

### 4.3 Android Manifest

- `android/app/src/main/AndroidManifest.xml`: إضافة `<uses-permission android:name="android.permission.POST_NOTIFICATIONS"/>` (مطلوبة لـ `FirebaseMessaging.requestPermission()` على Android 13+).

### 4.4 كود Dart جديد (`security_mobile/lib/core/push/`)

| ملف | الدور |
|-----|------|
| `firebase_bootstrap.dart` | `FirebaseBootstrap`: تهيئة Firebase مرة واحدة فقط لكل عملية، **Android فقط** (لا `GoogleService-Info.plist` لـ iOS بعد — مؤجَّل R-04). `isSupportedPlatform` يحدد المنصّة المدعومة؛ `isInitialized` تشخيصي فقط ولا يُستخدم لاختيار المنفذ (انظر §5). |
| `firebase_messaging_gateway.dart` | `FirebaseMessagingGateway` (واجهة ضيقة) + `DefaultFirebaseMessagingGateway` (التنفيذ الحقيقي فوق `FirebaseMessaging.instance`) — تفصل `FirebaseFcmMessaging` عن القنوات الحقيقية ليُمكن اختباره بـ fakes بلا جهاز. |
| `firebase_fcm_messaging.dart` | `FirebaseFcmMessaging implements FcmMessagingPort` — التنفيذ الحقيقي؛ كل استدعاء محميّ بـ try/catch يُعيد النتيجة الموثّقة (null/false/فارغ) عند الفشل، ويُسجِّل الخطأ بـ `debugPrint` دون أي توكن خام. |
| `firebase_messaging_background_handler.dart` | `firebaseMessagingBackgroundHandler` — نقطة دخول معزولة (`@pragma('vm:entry-point')`) لتسجيلها عبر `FirebaseMessaging.onBackgroundMessage`؛ تُعيد تهيئة Firebase داخل isolate الخلفية فقط، بلا أي لمس للواجهة (قيد FCM-C-05). |

### 4.5 الربط (DI) والتشغيل

- `notifications_providers.dart`: `fcmMessagingPortProvider` يُرجع `FirebaseFcmMessaging()` عندما `FirebaseBootstrap.isSupportedPlatform` فقط (Android)، وإلا `NoOpFcmMessaging()` — **القرار بالمنصّة لا بنجاح/فشل التهيئة**، كي لا يتحول التنفيذ صامتاً إلى NoOp عند فشل Firebase على Android (مطلب صريح في `fcm-completion-tasks.md`).
- `main.dart`: `await FirebaseBootstrap.ensureInitialized()` قبل `runApp`؛ تسجيل `FirebaseMessaging.onBackgroundMessage(...)` فقط إذا نجحت التهيئة فعلاً.

### 4.6 قرار هندسي مسجَّل — الإبقاء على API المُهمَل

`DefaultFirebaseMessagingGateway` يستخدم عمداً `getToken()`/`onTokenRefresh` المُعلَّمين `@Deprecated` في `firebase_messaging` 17.x (مع `// ignore: deprecated_member_use` موضَّح بالسبب في الكود)، بدل `register()`/`onRegistered` الجديدين. السبب: `onRegistered` يُرجع Firebase Installation ID لا توكن FCM تقليدي، والتبديل كان سيغيّر ضمنياً ما تستقبله خدمة `/api/me/push-tokens` ويُرسله `FirebaseFcmProvider` عبر Admin SDK (`messages:send` بحقل `token`) — وهو تغيير عقد عبر-مشاريع يحتاج مراجعة `integration` ولا يجوز اتخاذه ضمن مهمة عميل منفردة. **لم يُغيَّر عقد Backend/Mobile.**

## 5) لماذا لا يوجد "تحوّل صامت إلى NoOp"

`fcmMessagingPortProvider` لا يتحقق من نجاح `Firebase.initializeApp()` عند اختيار المنفذ — فقط من كون المنصّة Android. فإذا فشلت التهيئة فعلياً على جهاز Android حقيقي (سيناريو مستقبلي)، يبقى المنفذ `FirebaseFcmMessaging` نفسه هو المُرجَع، وكل استدعاء داخله (`getToken`, `requestPermission`, إلخ) سيفشل **بشكل مرئي في السجلات** (`debugPrint` لكل استثناء) ويُعيد القيمة الموثّقة (null/false) — لا تبديل صامت لكائن آخر يخفي الفشل. هذا يحقق الشرط الحرفي "عدم التحول الصامت إلى NoOp عند حدوث خطأ في Android".

## 6) الاختبارات المنفَّذة والنتائج الفعلية

| الاختبار / الأمر | النتيجة |
|------------------|---------|
| `flutter analyze` (كامل المشروع) | **0 مشاكل** (بعد تصحيح استيراد `AuthorizationStatus` واحتواء تحذيرات deprecation بالتوثيق) |
| `test/core/push/firebase_fcm_messaging_test.dart` (16 حالة) | **16/16 Passed** — permission (authorized/provisional/denied/throw) · getToken (قيمة/فارغ/null/throw) · onTokenRefresh (تمرير + ابتلاع الخطأ دون تسريبه) · getInitialMessageData (map/null/throw) · onNotificationOpened (تمرير + ابتلاع الخطأ) |
| `test/core/push/firebase_bootstrap_test.dart` (4 حالات) | **4/4 Passed** — على هذا المضيف (Windows، بلا Android) `isSupportedPlatform=false` دائماً، وهذا هو السلوك المُختبَر فعلاً لا قصوراً في الاختبار |
| `test/features/notifications/fcm_messaging_port_provider_test.dart` (1 حالة) | **Passed** — يتحقق أن المزوّد يُرجع `NoOpFcmMessaging` على منصّة غير مدعومة (هذا المضيف) |
| **كامل مجموعة اختبارات `security_mobile`** (63 حالة قبل C-03 + 21 جديدة) | **84/84 Passed, 0 Failed** — لا تراجع في أي من: auth session, auth guard, auth interceptor, devices, wifi provisioning, acceptance smoke, open-notification usecase, shell tabs, widget test |

جميع الـ fakes المستخدَمة في `firebase_fcm_messaging_test.dart` تُنفِّذ `FirebaseMessagingGateway` مباشرة (نفس أسلوب الاختبارات الحالية في المشروع — Fake classes، لا Mockito)، ولا تستخدم أي توكن حقيقي أو قيمة مُختلَقة لأغراض إرسال حي — قيم نصّية اصطناعية داخل اختبارات الوحدة فقط.

## 7) ما لم يُثبَت — بناء APK

| بند | الحالة |
|-----|--------|
| `flutter build apk --debug --flavor dev` | **غير مكتمل** — حاولت مرتين؛ المحاولة الأولى استمرت ~95 دقيقة ثم أوقفتُها بعد أدلة توقّف فعلي (تفاصيل §8)؛ المحاولة الثانية (verbose) أُوقفت بطلب المالك بسبب ضعف الشبكة لديه |
| `flutter build apk --debug --flavor staging` | **لم تُجرَّب** (معلَّق على نتيجة dev) |
| `flutter build apk --debug --flavor prod` | **لم تُجرَّب** |
| التحقق من موارد `google-services` المولَّدة فعلياً لكل flavor | **لم يُثبَت ببناء ناجح** — الربط في Gradle صحيح بنيوياً (الإضافة مُفعَّلة، الملفات في المسارات الصحيحة من FCM-C-01) لكن لم تُشاهَد نتيجة دمج حقيقية |

**لم أدّعِ نجاح بناء لم يحدث.** هذا شرط قبول صريح لـ FCM-C-03 ("البناء حسب flavor يلتقط google-services الصحيح") **غير مُحقَّق بالدليل حتى الآن** — السبب خارجي (شبكة المالك)، لا خلل مكتشف في الربط نفسه.

## 8) تحليل محاولة البناء الأولى (شفافية كاملة)

رُصدت المحاولة الأولى لمدة ~95 دقيقة قبل الإيقاف. الأدلة المجمَّعة أثناء المراقبة:

- اختبار شبكة مباشر: `dl.google.com` استجاب خلال ~10 ثانية لملف POM واحد، `repo.maven.apache.org` خلال ~6 ثانية — زمن استجابة مرتفع بشكل غير طبيعي لكل طلب.
- كاش Gradle المحلي (`~/.gradle/caches/modules-2/files-2.1/com.google.firebase`) أظهر 13 مكتبة Firebase منزَّلة فعلياً — تقدّم حقيقي في مرحلة أولى.
- عند الفحص اللاحق (~45 دقيقة بعد ذلك): توقف تحديث كاش `com.google.firebase`/`com.google.android.gms`/`androidx.core` تماماً منذ ~08:11-08:13، واستهلاك CPU لعملية Gradle daemon لم يتجاوز ~25 ثانية إضافية خلال 17 دقيقة مراقبة لاحقة — مؤشر توقف فعلي بعد انتهاء التنزيل، لا استمرار بطء شبكي فقط.
- أُوقفت شجرة العمليات (`gradlew.bat` + Gradle daemon + Kotlin compile daemon) وأُعيد البناء بوضع `-v` لمزيد من الشفافية، ثم أوقفه المالك بسبب ضعف شبكته قبل ظهور نتيجة.

لم تُحذف أي بيانات مشروع أو تعديلات كود بسبب هذا الإيقاف — فقط عمليات Gradle/Dart مؤقتة لأمر بناء لم يكتمل.

## 9) الملفات المضافة / المعدَّلة

| مسار | الإجراء |
|------|---------|
| `security_mobile/pubspec.yaml` | إضافة `firebase_core: ^5.1.0`, `firebase_messaging: ^17.0.1` |
| `security_mobile/pubspec.lock` | تحديث تلقائي من `flutter pub add` |
| `security_mobile/android/settings.gradle.kts` | إضافة google-services plugin |
| `security_mobile/android/app/build.gradle.kts` | تطبيق google-services plugin |
| `security_mobile/android/app/src/main/AndroidManifest.xml` | إضافة صلاحية `POST_NOTIFICATIONS` |
| `security_mobile/lib/core/push/firebase_bootstrap.dart` | **جديد** |
| `security_mobile/lib/core/push/firebase_messaging_gateway.dart` | **جديد** |
| `security_mobile/lib/core/push/firebase_fcm_messaging.dart` | **جديد** |
| `security_mobile/lib/core/push/firebase_messaging_background_handler.dart` | **جديد** |
| `security_mobile/lib/features/notifications/presentation/providers/notifications_providers.dart` | تعديل `fcmMessagingPortProvider` فقط |
| `security_mobile/lib/main.dart` | إضافة تهيئة Firebase + تسجيل معالج الخلفية |
| `security_mobile/test/core/push/firebase_fcm_messaging_test.dart` | **جديد** (16 حالة) |
| `security_mobile/test/core/push/firebase_bootstrap_test.dart` | **جديد** (4 حالات) |
| `security_mobile/test/features/notifications/fcm_messaging_port_provider_test.dart` | **جديد** (1 حالة) |
| `docs/fcm/fcm-completion-tasks.md` | إغلاق C-02 + تحديث حالة C-03 |

**لم يُعد بناؤه:** `FcmMessagingPort` (العقد)، `PushTokenLifecycle`، `OpenNotificationUseCase`، `PushTokensRepository`/`PushTokensRemoteDataSource`، شاشات الإشعارات.

## 10) ملاحظة جانبية مكتشَفة (خارج نطاق C-03، غير مُعالَجة)

`android/app/src/debug/AndroidManifest.xml` يحتوي `android.permission.INTERNET` بينما **لا توجد هذه الصلاحية في `src/main/AndroidManifest.xml`** — ما يعني أن بناء `release` (وهو buildType المستخدَم فعلياً لكل الـ flavors عبر `signingConfig = debug` الحالي) قد يفتقد صلاحية الشبكة عند التشغيل الفعلي على جهاز. هذا موجود قبل FCM-C-03 ولا علاقة له بالإشعارات تحديداً (يؤثر على كل اتصال HTTP عبر `dio`)، ولم أُعدِّله لأنه خارج نطاق المهمة المصرَّح به ويحتاج مراجعة/تفويض مستقل. **تمت إضافته إلى `fcm-remaining-tasks.md` للمتابعة.**

## 11) شروط القبول

| الشرط | الحالة |
|-------|--------|
| إضافة `firebase_core`/`firebase_messaging` بإصدارات متوافقة | **محقَّق** |
| تسجيل google-services plugin دون كسر flavors | **محقَّق بنيوياً** (لم يُثبَت ببناء ناجح — §7) |
| تنفيذ `FirebaseFcmMessaging` يحقق `FcmMessagingPort` | **محقَّق** |
| حقن التنفيذ بدل NoOp (Android) | **محقَّق** |
| `flutter analyze` + اختبارات وحدة بلا جهاز | **محقَّق** (0 مشاكل، 21 اختبار جديد ناجح، 84/84 إجمالي) |
| البناء حسب flavor يلتقط `google-services` الصحيح | **غير محقَّق بالدليل** — مؤجَّل بطلب المالك (شبكة) |
| المنفذ لم يعد NoOp على Android (منطقياً في الكود) | **محقَّق في الكود**؛ **غير مُثبَت على جهاز/بناء حقيقي** |
| لا Service Account داخل التطبيق | **محقَّق** (لم تُلمس أي ملفات SA؛ `google-services.json` ليست Service Account) |

## 12) الحكم

**SOFTWARE COMPLETE — BUILD VERIFICATION PENDING.** الكود والاختبارات والربط البنيوي مكتملة ومُحقَّقة فعلياً (analyze + 84 اختبار ناجح). التحقق النهائي ببناء APK فعلي لكل flavor **مؤجَّل بطلب صريح من المالك** بسبب ضعف الشبكة أثناء الجلسة — لا يجوز إغلاق البطاقة `[x]` قبل توفر هذا الدليل. سأستمر الآن إلى FCM-C-04 لأن اعتمادياته البرمجية (وجود `FcmMessagingPort`/`FirebaseFcmMessaging` في الكود) متحققة، وفق تفويض التنفيذ التسلسلي الذي يسمح بإكمال الأعمال البرمجية المستقلة عن العائق الخارجي.

---

## 🧠 مقتطف هندسي من المهمة

**المفهوم:** عزل SDK خارجي (Firebase Messaging) خلف واجهة ضيقة خاصة بالتطبيق (`FirebaseMessagingGateway`) بدل استدعاء `FirebaseMessaging.instance` مباشرة داخل منطق التطبيق.

**لماذا:** مكتبات SDK الخاصة بالأجهزة (Firebase، الكاميرا، الموقع...) تعتمد على قنوات اتصال أصلية (platform channels) لا تعمل في بيئة اختبار Dart عادية بلا جهاز/محاكٍ، وتتغيّر توقيعاتها بين الإصدارات (كما حدث هنا: `getToken()` أُهمِل لصالح `register()` في الإصدار 17.x بدلالة مختلفة جوهرياً — Installation ID لا FCM token). لو استُدعيت المكتبة مباشرة من `FirebaseFcmMessaging`، لتعذّر اختبار منطق معالجة الأخطاء وتحويل البيانات بلا جهاز حقيقي.

**كيف يعمل:** `FirebaseMessagingGateway` تُعرِّف فقط التوابع التي يحتاجها `FcmMessagingPort` (5 توابع/تيارات)، وتُنفَّذ بنسختين: `DefaultFirebaseMessagingGateway` (حقيقية، فوق SDK) تُستخدَم في الإنتاج، وFakes مباشرة في الاختبارات (`_FakeGateway` في `firebase_fcm_messaging_test.dart`) تُحاكي كل سيناريو (قيمة صحيحة، قيمة فارغة، استثناء، بث خطأ) دون أي اعتماد على جهاز.

**متى نستخدمه:** في أي مشروع يغلّف SDK خارجي (دفع، دفعات، خرائط، بصمة...) يُراد اختبار منطق التطبيق المحيط به (تحويل بيانات، معالجة أخطاء، قواعد أمان) دون تشغيل المكتبة الحقيقية في كل اختبار وحدة.

**كيف نتحقق عملياً:** شغّلنا `flutter test test/core/push/firebase_fcm_messaging_test.dart` على بيئة Windows بلا Android SDK فعلياً أثناء التشغيل (لا محاكي، لا جهاز) ونجحت 16 حالة تغطي النجاح والفشل والبث والأخطاء — إثبات مباشر أن الفصل يعمل: لو كان الاستدعاء مباشراً لـ `FirebaseMessaging.instance` لفشلت هذه الاختبارات فوراً بخطأ `MissingPluginException` لغياب قناة التلقي الأصلية.
