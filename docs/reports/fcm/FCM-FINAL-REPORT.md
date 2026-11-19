# التقرير النهائي — مرحلة استكمال Firebase Cloud Messaging (FCM-C-01…C-07)

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-19 |
| النطاق | `security_backEnd` + `security_mobile` فقط |
| الحكم النهائي | **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING** |
| عدّاد مرحلة FCM (مستقل) | **2 / 7 مُغلَقة رسمياً** (C-01, C-02) · **4 أخرى SOFTWARE COMPLETE بانتظار موافقتك** (C-03…C-06) · C-07 هذا التقرير |
| عدّاد النظام الرسمي | **21/50 — بلا تغيير** (هذه المرحلة مستقلة تماماً) |

---

## Addendum — مراجعة مالك لاحقة (2026-11-19، نفس اليوم، بعد التقرير الأصلي)

بتفويض صريح لاحق من المالك ("FCM — Final Software Completion Review")، نُفِّذ استكمال إضافي محدَّد:

1. **FCM-R-04 (صلاحية INTERNET):** أُضيفت إلى `src/main/AndroidManifest.xml` وتحقَّقت دمجها فعلياً في الـ merged manifest الوسيط لنسخة `devDebug` — انظر `FCM-C-03-report.md` §10.
2. **محاولات بناء APK إضافية (4 محاولات):** تجاوزت مرحلة التنزيل وكشفت فجوتي ضبط Gradle حقيقيتين (`isCoreLibraryDesugaringEnabled` + `desugar_jdk_libs:2.1.4`) — **أُصلِحتا فعلاً**. كُشف أيضاً تعليق حقيقي في عملية Gradle (daemon عالق، لا شبكة) وتم تطهيره. **لم يُثبَت نجاح بناء كامل حتى الآن** — المحاولة الأخيرة بعد الإصلاحين أُوقفت بطلب المالك قبل اكتمالها. التفاصيل الكاملة: `FCM-C-03-report.md` §8A.
3. **مراجعة تكميلية لـ `LocalNotificationsService`:** بند بندي كامل — راجع `FCM-C-05-report.md` §7A. أهم نتيجة: `onDidReceiveBackgroundNotificationResponse` المُسجَّل `no-op` **مؤكَّد صحيحاً من مصدر توثيق الحزمة نفسها** (لا يُستدعى إلا لإجراءات إشعار لا تفتح الواجهة؛ لا إجراءات من هذا النوع مُعرَّفة) — لم يعد بنداً غامضاً يحتاج جهازاً، بل خلص فعلياً.
4. **دمج `fcm-remaining-tasks.md`:** أُعيدت هيكلته بالكامل — 12 بنداً (FCM-R-01…R-10 مع بنود فرعية B) مُدمَجة ومربوطة بالمراجع الأصلية (`mobile-remaining-tasks.md` R-04/R-07/R-08) دون تكرار وصف، مع إضافة بندين كانا غائبين صراحة: اختبار تعدد الأجهزة (R-07B) وسلسلة ESP32→MQTT→Backend→FCM→Mobile الكاملة (R-10).
5. **`FCM-ENVIRONMENTS.md`:** أُضيف قسم 7 يوثّق حالة ربط عميل Flutter الفعلية (حزم، Gradle plugin، الصلاحيتان، حالة بناء APK) بتاريخ اليوم.

**لا تغيير في الحكم النهائي أدناه** — لا يزال **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING**؛ بناء APK الناجح فعلياً يبقى الاعتماد الخارجي الوحيد المتبقي من ضمن ما كان مؤجَّلاً للشبكة (FCM-R-01…R-03B)، والإصلاحات المذكورة أعلاه صحيحة ومسجَّلة على القرص بانتظار محاولة بناء تُستكمَل دون إيقاف.

---

## Executive Summary

النظام البرمجي لإشعارات FCM مكتمل **من ناحية الكود والاختبارات الآلية** في كل من Backend وMobile:

- **Backend**: `FirebaseFcmProvider` يعمل بمصادقة ADC حقيقية (dev فقط)، مع تصنيف أخطاء صحيح (بعد إصلاح فجوة `ThirdPartyAuthError` في FCM-C-06)، وعامل إرسال (`NotificationDispatchService`) محمي بالكامل من السباقات والتكرار اللانهائي وإعادة ربط الإشعار بمالك سابق.
- **Mobile**: `FirebaseFcmMessaging` تنفيذ حقيقي بدل `NoOpFcmMessaging` على Android، مع دورة حياة توكن محمية من سباقات تبديل الحساب (فجوة حقيقية اكتُشفت وأُصلحت)، وعرض إشعارات محلي (`flutter_local_notifications`) بعد اكتشاف أن Backend يرسل حمولة بيانات فقط لا تُعرَض تلقائياً على Android في أي حالة تشغيل.

**ما بقي غير مُثبَت** هو تماماً ما كان متوقَّعاً أن يبقى خارج نطاق جلسة بلا جهاز حقيقي وبشبكة غير مستقرة: بناء APK ناجح فعلياً، توكن/استقبال حقيقي على جهاز، وتأكيد يدوي من Google Cloud Console. لا ادّعاء بأي من هذه البنود في أي تقرير فرعي.

## Tasks Status

| المهمة | الحالة | الدليل |
|--------|--------|--------|
| FCM-C-01 | **CLOSED** 2026-11-19 | `FCM-C-01-report.md` |
| FCM-C-02 | **CLOSED** 2026-11-19 (موافقتك الصريحة في بداية هذه الجلسة) | `FCM-C-02-report.md` + الملحق |
| FCM-C-03 | **SOFTWARE COMPLETE — BUILD VERIFICATION PENDING** | `FCM-C-03-report.md` |
| FCM-C-04 | **SOFTWARE COMPLETE** | `FCM-C-04-report.md` |
| FCM-C-05 | **SOFTWARE COMPLETE** | `FCM-C-05-report.md` |
| FCM-C-06 | **SOFTWARE COMPLETE** | `FCM-C-06-report.md` |
| FCM-C-07 | **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING** | `FCM-C-07-report.md` (هذا الملف) |

**لا بطاقة منها مُعلَّمة `[x]` في `fcm-completion-tasks.md`** — القاعدة المعلنة في بداية المرحلة ("لا تُعلَّم [x] قبل موافقة صريحة") مُطبَّقة حرفياً حتى لهذه المهام المكتملة برمجياً.

## Requirements Traceability

| المتطلب الأصلي | التنفيذ | الدليل |
|-----------------|---------|--------|
| FCM-03 (خطة التطوير): تسجيل/تحديث/إبطال التوكن، منع إرسال لمالك سابق | `PushTokenLifecycle` + `PushTokensRepositoryImpl` (FCM-C-04) + `PushTokenService.RegisterOrUpdateAsync` (BE-T-09، مُراجَع لا مُعاد بناء) | `FCM-C-04-report.md` §2-4، اختبارات `push_token_lifecycle_test.dart` |
| FCM-05 (خطة التطوير): حمولة قليلة البيانات، لا تفاصيل حساسة على شاشة القفل | `FcmPayloadBuilder` (Backend، غير مُعدَّل) + `LocalNotificationsService` (Mobile، نص عام ثابت) | `FCM-C-05-report.md` §2-3 |
| FCM-06 (خطة التطوير): قبول fg/bg/terminated + إذن معطَّل + نقل ملكية | `NotificationOpenCoordinator` + `ForegroundAlertCoordinator` (Mobile) + `NotificationRecipientGuard`/`Ownership_Transfer_During_Queue_...` (Backend، مُراجَع) | `FCM-C-05-report.md`, `FCM-C-06-report.md` §2.7/§3.1 |
| BE-T-09: عامل إرسال، تسجيل محاولة، إعادة محاولة عند فشل مؤقت | موجود مسبقاً، رُوجِع بعمق في FCM-C-06، فجوة تصنيف واحدة أُصلِحت | `FCM-C-06-report.md` §2.1 |
| MOB-T-06: عميل HTTP + NoOp (كان مؤجَّلاً لـ FCM-C-03) | استُبدل التنفيذ فقط؛ العقد زاد إضافياً (`onForegroundMessage`) | `FCM-C-03-report.md`, `FCM-C-05-report.md` §3.1 |

## Backend Readiness

| البند | الحالة |
|-------|--------|
| `Fcm:Enabled`/`Provider` في Git | `false` / `Fake` — **بلا تغيير طوال المرحلة** |
| تفعيل Firebase حي في dev | مُحقَّق سابقاً (FCM-C-02)، جلسة محلية فقط، قاعدة معزولة `esp32_security_fcm_dev` |
| تصنيف أخطاء الإرسال | **مكتمل** بعد إصلاح FCM-C-06 (7/7 قيم `MessagingErrorCode` مُصنَّفة بوضوح) |
| حماية السباقات/التكرار/نقل الملكية في عامل الإرسال | **مؤكَّدة بالمراجعة** — لا تعديل مطلوب (كانت سليمة من BE-T-09) |
| staging / prod | **غير مفعَّلين إطلاقاً** |
| Build | **Build succeeded — 0/0** |
| Application.Tests | **114/114 Passed** |
| API.IntegrationTests | **161/182 Passed, 21 Skipped (غير متعلق بـ FCM), 0 Failed** |

## Mobile Readiness

| البند | الحالة |
|-------|--------|
| `FcmMessagingPort` | واجهة مكتملة (+إضافة `onForegroundMessage`)، منفَّذة بالكامل لـ Android (`FirebaseFcmMessaging`) |
| `PushTokenLifecycle` | محمي من سباقات تبديل الحساب (فجوة أُصلِحت)، إعادة محاولة شبكية، لا ادّعاء إبطال كاذب |
| عرض الإشعارات (fg/bg/terminated) | مكتمل برمجياً عبر `LocalNotificationsService` + `NotificationOpenCoordinator` + `ForegroundAlertCoordinator`؛ حارس جاهزية Bootstrap + منع فتح مكرر |
| `flutter analyze` | **0 مشاكل** |
| `flutter test` | **76/76 Passed, 0 Failed** |
| بناء APK (dev/staging/prod) | **غير مكتمل** — شبكة ضعيفة أثناء الجلسة، مؤجَّل بطلب المالك |
| iOS | **خارج النطاق بالكامل** (قرار سابق للمرحلة، `GoogleService-Info.plist` غير موجود) |

## Firebase Environments

| البيئة | الحالة البرمجية | الحالة التشغيلية |
|--------|-------------------|---------------------|
| dev (`smart-security-dev-28642`) | مُهيَّأة بالكامل في Git (google-services.json + Gradle plugin) | تفعيل Backend مُحقَّق محلياً (C-02)؛ إرسال حي غير مُحقَّق؛ تفعيل FCM API في Console **غير مؤكَّد آلياً (403)** |
| staging (`smart-security-stage`) | ملف العميل موجود، Gradle plugin يُطبَّق على كل الـ flavors | **غير مُفعَّلة إطلاقاً** |
| prod (`smart-security-pro-fbdeb`) | ملف العميل موجود، Gradle plugin يُطبَّق على كل الـ flavors | **غير مُفعَّلة إطلاقاً** |
| iOS (الثلاث بيئات) | **غير مُهيَّأة** — لا `GoogleService-Info.plist` | مؤجَّل بالكامل |

## Testing Summary (أرقام فعلية فقط)

| الحزمة | Passed | Failed | Skipped | Total |
|--------|--------|--------|---------|-------|
| Backend `Application.Tests` | 114 | 0 | 0 | 114 |
| Backend `API.IntegrationTests` | 161 | 0 | 21 | 182 |
| Mobile `flutter test` (كامل المجلد) | 76 | 0 | 0 | 76 |
| Mobile `flutter analyze` | — | 0 مشاكل | — | — |
| Backend `dotnet build Backend.sln -c Release` | Build succeeded | 0 Warning | 0 Error | — |

**الاختبارات الجديدة المضافة في هذه المرحلة (Mobile):** 35 حالة جديدة عبر 7 ملفات (`firebase_fcm_messaging_test.dart` +18 كلي، `firebase_bootstrap_test.dart` +4، `fcm_messaging_port_provider_test.dart` +1، `push_tokens_repository_impl_test.dart` +5، `push_token_lifecycle_test.dart` +2، `notification_open_coordinator_test.dart` +3، `foreground_alert_coordinator_test.dart` +1) — جميعها ناجحة، صفر تعديل/حذف لاختبار سابق.

**الاختبارات المعدَّلة في Backend:** صفر اختبارات جديدة (الإصلاح كان في كود تصنيف الأخطاء فقط، لا مشروع اختبار وحدة متاح لهذه الدالة الخاصة — موضَّح في `FCM-C-06-report.md` §2.1)؛ 0 اختبار فاشل بعد الإصلاح.

## Security Verification

ملخَّص من `FCM-C-06-report.md`:

- **فجوة حقيقية واحدة أُصلِحت:** تصنيف `MessagingErrorCode.ThirdPartyAuthError` (كان يسقط ضمن Retryable الافتراضي؛ أصبح Failed بلا إبطال توكن).
- لا تسريب توكن FCM/JWT في أي سجل عبر كامل كود هذه المرحلة (فُحص مباشرة).
- حماية ملكية مزدوجة مؤكَّدة: الخادم يُعيد فحص الملكية عند كل إرسال (`NotificationRecipientGuard`) وعند كل طلب تفاصيل (`GetDetail` بمعرّف المستخدم الحالي من JWT) — مستقلتان تماماً.
- عرض الإشعار المحلي (Mobile) نص عام ثابت بالكامل، لا تفاصيل حادثة في أي نص مرئي على شاشة القفل.
- لا تغيير في `TokenEncryptionKey`، لا تفعيل staging/prod، لا تعديل IAM.

## Remaining Tasks (مرجعاً لـ `docs/fcm/fcm-remaining-tasks.md`)

9 بنود (FCM-R-01…R-09) — أهمها: بناء APK لكل flavor (شبكة)، توكن/استقبال حقيقي على جهاز (fg/bg/terminated)، تأكيد يدوي لتفعيل FCM API في Google Cloud Console، وصلاحية `android.permission.INTERNET` المكتشَفة ناقصة من `src/main/AndroidManifest.xml` (مشكلة مستقلة عن FCM، مُسجَّلة لتفويض منفصل). القائمة الكاملة بالتفاصيل والاعتماديات في الملف المذكور.

## Regression Verification

- **Backend**: 161/182 (API.IntegrationTests) و114/114 (Application.Tests) — **مطابقان تماماً** لأرقام ما قبل هذه المرحلة (FCM-C-02)، سوى إصلاح FCM-C-06. لا Migration جديدة، لا تعديل في Auth/Ownership/MQTT.
- **Mobile**: 76/76 — تراكمي بالإضافة فقط من أول تشغيل كامل رُصِد (63) عبر كل مهمة (+7 C-04، +6 C-05)، بلا حذف أي اختبار. `app_router.dart` تعديل إضافي بحت (`navigatorKey`)، لا تغيير في أي `GoRoute` موجود.
- **Shared Core / Web / ESP**: لم تُلمَس، لا حاجة لإعادة اختبار.

## Final Verdict

### SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING

جميع الأعمال البرمجية الممكنة دون جهاز Android حقيقي، شبكة مستقرة لبناء Gradle، أو تفويض Google Cloud IAM **اكتملت فعلياً واختُبرت بأدلة قابلة لإعادة الإنتاج** (الأعداد أعلاه). الاعتماديات الخارجية المتبقية (بناء APK، جهاز حقيقي، تأكيد Console) موثَّقة صراحة في `fcm-remaining-tasks.md` ولا تُعتبَر نقصاً في التنفيذ البرمجي.

**لا يُستخدَم حكم PRODUCTION READY** — شروط القبول التشغيلي والميداني (استقبال حي، بناء ناجح، تأكيد Console) غير مُحقَّقة بالدليل.

---

**بانتظار مراجعتك النهائية والموافقة الصريحة على إغلاق أي من بطاقات FCM-C-03…C-07، كما طُلِب في أمر البدء.**
