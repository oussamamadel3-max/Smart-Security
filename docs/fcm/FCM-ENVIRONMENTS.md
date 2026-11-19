# مصفوفة بيئات Firebase Cloud Messaging

آخر تحديث: 2026-11-19 (FCM-C-01 — تحقق وتوثيق فقط؛ الإرسال الحي وSDK Flutter غير مفعّلين)

لا تضع Service Account أو مفاتيح APNs الخاصة في هذا الملف أو في Git.

## 1) عزل المشاريع

ثلاثة مشاريع Firebase **مستقلة** (ليس مشروعاً واحداً بثلاث تطبيقات فقط):

| بيئة التشغيل | Firebase `project_id` | `project_number` | Android Application ID | مسار `google-services.json` |
|--------------|------------------------|------------------|-------------------------|------------------------------|
| **dev** | `smart-security-dev-28642` | `313416905013` | `com.hisn.security.security_mobile.dev` | `security_mobile/android/app/src/dev/google-services.json` |
| **staging** | `smart-security-stage` | `19080112160` | `com.hisn.security.security_mobile.staging` | `security_mobile/android/app/src/staging/google-services.json` |
| **prod** | `smart-security-pro-fbdeb` | `540692592111` | `com.hisn.security.security_mobile` | `security_mobile/android/app/src/prod/google-services.json` |

Flavor Gradle المعتمد في `build.gradle.kts` هو الاسم **`prod`** (وليس `production`). يجب أن يعيش ملف الإنتاج تحت `src/prod/` حتى يُدمَج في source set الخاص بالـ flavor.

### منع خلط البيئات

1. **عميل Android:** كل flavor يحمّل `google-services.json` من source set الخاص به فقط → Application ID مختلف → تسجيل FCM على مشروع Firebase مختلف.
2. **خادم Backend:** ملف Service Account واحد لكل بيئة؛ يُضبط عبر `GOOGLE_APPLICATION_CREDENTIALS` قبل تشغيل العملية. لا تُخلط ملفات SA بين بيئات التشغيل.
3. **إعدادات التطبيق:** `APP_ENV` / `API_BASE_URL` عبر `--dart-define` يجب أن تطابق الـ flavor (dev→dev API، إلخ). عدم تشغيل APK `prod` ضد API تطويري عند اختبار الإشعار الحي لاحقاً.
4. **القواعد/الطوابير:** بعد تفعيل الإرسال (FCM-C-02) يبقى `Fcm:Provider` و`Fcm:Enabled` خاصّين بنسخة النشر؛ الاختبارات الآلية تبقى على `Fake`.
5. **مشروع قديم:** وُجد سابقاً مشروع/اعتماد باسم `smart-security-80d2c` خارج الثلاثي الحالي — **لا يُستخدم** مع flavors الحالية.

## 2) Service Account (خارج المستودع)

المسار الفعلي المُتحقق منه على جهاز المالك:

`C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\`

| بيئة | مجلد فرعي | تطابق `project_id` مع عميل Android |
|------|-----------|-------------------------------------|
| dev | `...\firebase\dev\` | نعم — `smart-security-dev-28642` |
| staging | `...\firebase\staging\` | نعم — `smart-security-stage` |
| prod | `...\firebase\production\` | نعم — `smart-security-pro-fbdeb` |

ملاحظة مسار: نص التفويض ذكر `C:\Secrets\SmartSecurity\Firebase`؛ هذا المسار **غير موجود** على الجهاز عند التحقق. المسار أعلاه هو المعتمد للتوثيق حتى يُنشأ مسار موحّد لاحقاً (اختياري عبر junction/نقل يدوي من المالك).

أسماء الملفات تحتوي `firebase-adminsdk` وتطابق أنماط `security_backEnd/.gitignore`. لا تُنسَخ إلى شجرة المستودع.

أرشيف محلي (خارج Git) لم依赖性 قديم نُقل من مجلد Backend أثناء FCM-C-01:

`C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\legacy\`

## 3) إعداد `GOOGLE_APPLICATION_CREDENTIALS` (للاستخدام في FCM-C-02)

لا تغيّر الآن `Fcm:Enabled` أو `Fcm:Provider` في `appsettings.json` (يبقين `false` / `Fake`).

### PowerShell — جلسة حالية (مثال dev)

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\dev\<SA_FILE_NAME>.json"
# ثم تشغيل API من مجلد المشروع بعد ضبط بقية الأسرار (JWT / TokenEncryptionKey) عبر User Secrets
```

استبدل `<SA_FILE_NAME>` باسم ملف الـ SA داخل مجلد البيئة (يظهر محلياً فقط؛ لا يُلصق في Git).

### staging / prod

نفس المتغير مع مسار مجلد `staging` أو `production` تحت مجلد الأسرار أعلاه. شغّل نسخة Backend واحدة بملف SA واحد فقط في كل عملية.

### User Secrets / بيئة نشر

- محلياً: يمكن الإبقاء على المتغير البيئي للجلسة أو توثيقه في ملف تشغيل شخصي خارج Git.
- نشر لاحق (ops): يُحقَن المسار أو محتوى السر عبر آلية أسرار المضيف — **ليس** عبر `appsettings.json` المتتبَّع.

`FirebaseFcmProvider` يستخدم Application Default Credentials (`GoogleCredential.GetApplicationDefault()`). عند `Fcm:Provider=Firebase` بدون ADC قابلة للحل، التسجيل يفشل عند الإقلاع عمداً.

## 4) حالة Backend الحالية (لا تغيير في FCM-C-01)

| مفتاح | القيمة في `API/appsettings.json` |
|-------|-----------------------------------|
| `Fcm:Enabled` | `false` |
| `Fcm:Provider` | `Fake` |
| حزمة | `FirebaseAdmin` 3.4.0 |

## 5) iOS وAPNs — مؤجّل

| البند | الحالة |
|-------|--------|
| `GoogleService-Info.plist` لكل بيئة | غير موجود |
| Bundle ID flavors منفصلة (`dev`/`staging`/`prod`) | غير مضبوطة؛ الحالي `com.hisn.security.securityMobile` فقط |
| مفتاح APNs في Firebase Console | غير مُثبت في هذه المرحلة |
| Capabilities / Push Notifications في Xcode | مؤجّل |
| بناء/اختبار iOS | مؤجّل بموافقة المالك (انظر أيضاً `mobile-remaining-tasks.md` R-04) |

Android flavors الثلاث جاهزة لربط العميل في **FCM-C-03**. iOS خارج نطاق الإغلاق الحالي لـ FCM-C-01.

## 6) ربط بمهام الاستكمال

| مهمة | استخدام هذه المصفوفة |
|------|----------------------|
| FCM-C-01 | تحقق المشاريع/الملفات/العزل (هذا المستند) |
| FCM-C-02 | ADC + تفعيل Backend مفوّض |
| FCM-C-03…C-05 | SDK Flutter + دورة الرمز + حالات التطبيق |
| FCM-C-06…C-07 | أمان/قبول |
