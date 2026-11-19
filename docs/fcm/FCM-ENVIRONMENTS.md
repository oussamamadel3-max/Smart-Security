# مصفوفة بيئات Firebase Cloud Messaging

آخر تحديث: 2026-11-19 (FCM-C-02 استكمال — قاعدة `esp32_security_fcm_dev` + تمييز OAuth عن صلاحية الإرسال؛ Git يبقى Fake)

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

## 3) تشغيل Backend مع Firebase (dev فقط — FCM-C-02)

**الملفات المتتبَّعة في Git تبقى دائماً:** `Fcm:Enabled=false` · `Fcm:Provider=Fake` · `Fcm:FirebaseProjectId=""`.

التفعيل الحي يتم **في جلسة محلية فقط** عبر متغيرات البيئة + User Secrets، وليس بتعديل `appsettings.json`.

### قاعدة بيانات معزولة لاختبارات Firebase الحية

| بند | قيمة |
|-----|------|
| الاسم | `esp32_security_fcm_dev` |
| الغرض | جلسات `Provider=Firebase` فقط — طابور فارغ افتراضياً |
| التهيئة | `dotnet ef database update` مع `ConnectionStrings__DefaultConnection` موجّه لهذه القاعدة |
| ممنوع | نسخ مستخدمين / push tokens من `esp32_security` |
| القاعدة العامة `esp32_security` | تبقى لعمل التطوير الاعتيادي خلف **Fake** |

جلسة سابقة قصيرة استخدمت `esp32_security` مع العامل الحي؛ مراجعة السجل أثبتت claim بلا صفوف (`0 entities written`) ولا محاولة Send. التشغيل اللاحق والسكربت يستخدمان المعزولة فقط.

### متطلبات مسبقة

1. `ASPNETCORE_ENVIRONMENT=Development` حتى تُحمَّل User Secrets (`Auth:Jwt:SigningKey`, `Fcm:TokenEncryptionKey`، وسلسلة الاتصال الأساسية).
2. `GOOGLE_APPLICATION_CREDENTIALS` → ملف SA داخل `...\firebase\dev\` فقط.
3. `Fcm__FirebaseProjectId=smart-security-dev-28642` (يجب أن يطابق `project_id` في ملف SA؛ وإلا يُرفض الإقلاع).
4. `Fcm__Provider=Firebase` و`Fcm__Enabled=true` للجلسة المفوضة.
5. `ConnectionStrings__DefaultConnection` يعيد توجيه `Database=` إلى **`esp32_security_fcm_dev`** (السكربت يتكفل بذلك).

### سكربت خارج المستودع

`C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\dev\run-backend-fcm-dev.ps1`

```powershell
powershell -File "C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\dev\run-backend-fcm-dev.ps1"
```

ملاحظات الإيقاف: `...\firebase\dev\stop-notes.txt`

### صلاحيات Firebase (ما أُثبت / ما يدوي)

| المستوى | الحالة |
|---------|--------|
| OAuth + `FirebaseApp` لـ `smart-security-dev-28642` | مُثبت |
| قراءة Service Usage / IAM عبر SA | **403** — غير متاح لهذا الحساب |
| تفعيل FCM HTTP v1 في Console | **تحقق يدوي للمالك** (APIs & Services → Firebase Cloud Messaging API) |
| `messages:send` بـ token جهاز | مؤجّل لمراحل لاحقة |

### إيقاف والعودة إلى Fake

1. أوقف العملية (`Ctrl+C` أو إنهاء العملية).
2. امسح متغيرات الجلسة:

```powershell
Remove-Item Env:GOOGLE_APPLICATION_CREDENTIALS, Env:Fcm__Enabled, Env:Fcm__Provider, Env:Fcm__FirebaseProjectId, Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
```

3. تشغيل `dotnet run` العادي بدون هذه المتغيرات يعود إلى Fake من `appsettings.json` وDB من User Secrets (`esp32_security`).

### staging / prod

**غير مفعّلين في FCM-C-02.** لا تضبط `Fcm__FirebaseProjectId` على مشاريع staging/prod في جلسة التطوير.

### حراسة خلط البيئات

عند `Provider=Firebase` يفرض التسجيل:
- وجود `Fcm:FirebaseProjectId`
- تطابق `project_id` في ملف `GOOGLE_APPLICATION_CREDENTIALS` مع القيمة المتوقعة
- نجاح ADC — **بدون** سقوط صامت إلى Fake

## 4) حالة Backend المتتبَّعة (Git)

| مفتاح | القيمة في `API/appsettings.json` |
|-------|-----------------------------------|
| `Fcm:Enabled` | `false` |
| `Fcm:Provider` | `Fake` |
| `Fcm:FirebaseProjectId` | `""` |
| حزمة | `FirebaseAdmin` 3.4.0 |

الاختبارات الآلية (`AuthApiFactory` وغيرها) تثبت `Fcm:Provider=Fake` و`Enabled=false`.

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
