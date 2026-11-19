# الأعمال المتبقية خارج مرحلة استكمال FCM-C (software-only)

ملف مستقل — **لا** يُحسب ضمن عدّاد النظام الرسمي (50) ولا ضمن عدّاد مرحلة FCM (مستقل، 7). يحتوي فقط على ما تعذّر إكماله أو إثباته ضمن FCM-C-01…C-07 حتى تاريخه. يُحدَّث أثناء التنفيذ التسلسلي ويُراجَع نهائياً في FCM-C-07.

| # | المعرّف | وصف العمل | السبب / الاعتماد | المهمة الأصلية | شرط القبول المستقبلي | الحالة |
|---|---------|-----------|-------------------|------------------|------------------------|--------|
| 1 | FCM-R-01 | بناء APK ناجح لـ flavor dev وتحقق دمج `google-services.json` فعلياً | شبكة المالك ضعيفة أثناء الجلسة — أُوقف البناء مرتين بطلبه | FCM-C-03 | بناء Gradle ناجح + تحقق موارد google-services المولَّدة | **NOT VERIFIED** |
| 2 | FCM-R-02 | بناء APK لـ flavor staging | معلَّق على FCM-R-01 | FCM-C-03 / FCM-C-07 | بناء ناجح | **BLOCKED BY EXTERNAL DEPENDENCY** |
| 3 | FCM-R-03 | بناء APK لـ flavor prod | معلَّق على FCM-R-01 | FCM-C-03 / FCM-C-07 | بناء ناجح | **BLOCKED BY EXTERNAL DEPENDENCY** |
| 4 | FCM-R-04 | إضافة `android.permission.INTERNET` إلى `src/main/AndroidManifest.xml` (موجودة حالياً فقط في `src/debug/`، وbuildType `release` قد يفتقدها على جهاز حقيقي) | خارج نطاق FCM-C-03 المصرَّح به؛ اكتُشف أثناء مراجعة Manifest ويحتاج تفويضاً مستقلاً لأنه يؤثر على كل اتصال HTTP لا الإشعارات فقط | اكتُشف أثناء FCM-C-03 | تعديل مُصرَّح به + بناء release يتحقق من الاتصال | **NOT STARTED** (يحتاج قرار/تفويض مالك) |
| 5 | FCM-R-05 | تأكيد يدوي أن Firebase Cloud Messaging API مفعّل في Console على `smart-security-dev-28642` | 403 عند محاولة التحقق الآلي عبر Service Usage API (من تقرير FCM-C-02 التكميلي) | FCM-C-02 | تحقق Console يدوي من المالك | **PENDING OWNER** |
| 6 | FCM-R-06 | توكن FCM حقيقي على جهاز Android | لا جهاز/محاكٍ ضمن قيود المرحلة | FCM-C-04 | جهاز حقيقي + إذن نظام فعلي | **BLOCKED — constraint §11** |
| 7 | FCM-R-07 | إرسال/استقبال رسالة Firebase حقيقية (foreground/background/terminated) عبر مسار `LocalNotificationsService` الجديد — بما فيها دقّة توقيت `onDidReceiveBackgroundNotificationResponse` (مسجَّل no-op حالياً لعدم القدرة على التحقق بلا جهاز) | لا جهاز + لا إرسال حي ضمن القيود | FCM-C-05 | جهاز حقيقي + مزوّد Firebase مفعّل + دليل ميداني لكل حالة (fg/bg/terminated) | **BLOCKED — constraint §11** |
| 8 | FCM-R-08 | iOS flavors + `GoogleService-Info.plist` + APNs | مؤجَّل بموافقة المالك سابقاً (R-04 في `mobile-remaining-tasks.md`) | — | بناء/اختبار iOS | **DEFERRED (pre-existing)** |
| 9 | FCM-R-09 | تفعيل staging/prod التشغيلي لـ Backend | خارج نطاق FCM-C-02 (dev فقط) | FCM-C-02 | تفويض مستقل لكل بيئة | **NOT STARTED** |

## تمييز صريح

- هذا الملف **لا يُغلق** أي بطاقة FCM-C ولا يُنشئ عدّاداً موازياً.
- لا يُضاف بند هنا لعمل مكتمل فعلياً وموثَّق بدليل في تقرير المهمة المعنية.
- يُراجَع هذا الملف ويُحدَّث مع كل تقرير FCM-C-0x لاحق، وتقرير نهائي عند FCM-C-07.
