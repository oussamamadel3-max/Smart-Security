# تقرير FCM-C-01 — مشاريع Firebase وبيئات التشغيل

| الحقل | القيمة |
|--------|--------|
| المهمة | **FCM-C-01** |
| التاريخ | 2026-11-19 |
| الحالة | **CLOSED** 2026-11-19 بموافقة `APPROVE TASK CLOSURE: FCM-C-01` |
| النطاق | تحقق + توثيق فقط |
| لم يُنفَّذ | FCM-C-02…C-07 · SDK Flutter · تفعيل إرسال Backend · جهاز/محاكٍ · iOS · Commit/Push |

مراجع: خطة §10 `FCM-01` · `BE-T-09` · `MOB-T-06` · `mobile-remaining-tasks.md` R-01 · [`docs/fcm/fcm-completion-tasks.md`](../../fcm/fcm-completion-tasks.md) · [`docs/fcm/FCM-ENVIRONMENTS.md`](../../fcm/FCM-ENVIRONMENTS.md)

---

## 1) الأعمال المنفذة

1. إنشاء ملف مهام المرحلة `docs/fcm/fcm-completion-tasks.md` (FCM-C-01…C-07) دون تغيير عدّاد النظام 50.
2. إنشاء مصفوفة البيئات وتشغيل ADC والعزل وتأجيل iOS في `docs/fcm/FCM-ENVIRONMENTS.md`.
3. التحقق من ملفات `google-services.json` ومطابقة `package_name` مع Application IDs.
4. استخراج `project_id` / `project_number` وإثبات استقلال المشاريع الثلاثة.
5. التحقق محلياً من Service Accounts خارج المستودع ومطابقة `project_id` (بدون عرض مفاتيح خاصة).
6. تصحيح مسار flavor الإنتاج: نقل الملف من `src/production/` إلى `src/prod/` ليطابق اسم الـ flavor في Gradle.
7. مراجعة `.gitignore` وسجل Git؛ نقل ملف SA قديم وُجد داخل `security_backEnd/` إلى مجلد أسرار خارج المستودع.
8. تأكيد بقاء `Fcm:Enabled=false` و`Fcm:Provider=Fake` وعدم وجود حزم Firebase في `pubspec.yaml`.

---

## 2) الملفات المضافة / المعدّلة

| مسار | الإجراء |
|------|---------|
| `docs/fcm/fcm-completion-tasks.md` | **جديد** — بطاقات المرحلة |
| `docs/fcm/FCM-ENVIRONMENTS.md` | **جديد** — مصفوفة/ADC/عزل/iOS |
| `docs/reports/fcm/FCM-C-01-report.md` | **جديد** — هذا التقرير |
| `security_mobile/android/app/src/dev/google-services.json` | موجود (وضعه المالك) |
| `security_mobile/android/app/src/staging/google-services.json` | موجود (وضعه المالك) |
| `security_mobile/android/app/src/prod/google-services.json` | نُقل من `src/production/` ليطابق flavor `prod` |
| `security_backEnd/.gitignore` | تحديث أنماط SA (انظر أدناه) |
| `security_mobile/mobile-remaining-tasks.md` | إشارة مرجعية لمرحلة FCM-C دون إغلاق R-01 |

**لم يُمس:** `appsettings.json` · تسجيل FCM · `pubspec.yaml` · كود Flutter/Backend الوظيفي.

---

## 3) مصفوفة تطابق البيئات

| بيئة | Firebase `project_id` | `project_number` | Android Application ID | `package_name` في الملف | SA `project_id` | النتيجة |
|------|------------------------|------------------|-------------------------|-------------------------|-----------------|---------|
| dev | `smart-security-dev-28642` | `313416905013` | `com.hisn.security.security_mobile.dev` | مطابق | مطابق | **PASS** |
| staging | `smart-security-stage` | `19080112160` | `com.hisn.security.security_mobile.staging` | مطابق | مطابق | **PASS** |
| prod | `smart-security-pro-fbdeb` | `540692592111` | `com.hisn.security.security_mobile` | مطابق | مطابق | **PASS** |

استقلال المشاريع: ثلاثة `project_id` وثلاثة `project_number` مختلفة — **PASS**.

Backend (لاحقاً عبر ADC): نفس الثلاثي أعلاه؛ الإرسال الحي **غير مفعّل** في هذه المهمة.

مسار الأسرار الفعلي: `C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\{dev|staging|production}\`  
(المسار المذكور في التفويض `C:\Secrets\SmartSecurity\Firebase` غير موجود على الجهاز عند التحقق.)

---

## 4) نتائج التحقق الأمني

| فحص | النتيجة |
|-----|---------|
| أنماط `*firebase-adminsdk*` / `*-adminsdk-*` / `serviceAccount*` في `security_backEnd/.gitignore` | موجودة |
| ملفات SA للثلاث بيئات داخل شجرة المستودع بعد المعالجة | **لا يوجد** |
| تتبّع Git لملفات `*adminsdk*` / service account | **غير متتبَّعة** |
| ظهورها في `git log` للمسارات المعنية | **لا** |
| ملف SA قديم `smart-security-80d2c-...json` كان موجوداً فعلياً تحت `security_backEnd/` (متجاهل من Git) | نُقل إلى `...\firebase\legacy\` خارج المستودع |
| ظهور `BEGIN PRIVATE KEY` كاعتماد حقيقي في الملفات المتتبَّعة | لا (فقط نصوص اختبار `Assert.DoesNotContain("private_key"...)`) |
| `Fcm:Provider` / `Enabled` | ما زالا Fake / false |
| عرض مفاتيح خاصة في التقرير | **لم يُعرض أي مفتاح خاص** |

`google-services.json` حالياً **untracked** (`??`). إعدادات عميل Firebase العامة ليست Service Account؛ قرار إضافتها لـ Git يبقى للمالك (لم يُنفَّذ Commit).

---

## 5) الأعمال المؤجلة

| بند | أين يُستكمل |
|-----|-------------|
| تفعيل `FirebaseFcmProvider` + ADC تشغيلي | FCM-C-02 |
| تثبيت FlutterFire واستبدال NoOp | FCM-C-03 |
| دورة Token حقيقية على جهاز | FCM-C-04 |
| fg / bg / terminated | FCM-C-05 |
| مراجعة أمان الإرسال الحي | FCM-C-06 |
| قبول نهائي للمرحلة | FCM-C-07 |
| iOS + APNs + Bundle ID flavors | مؤجّل صراحة (R-04 / قسم 5 في FCM-ENVIRONMENTS) |
| توحيد مسار الأسرار إلى `C:\Secrets\...` إن رغب المالك | يدوي خارج Git |
| Plugin `google-services` في Gradle | مع FCM-C-03 |

---

## 6) شروط القبول — FCM-C-01

| الشرط | الحالة |
|-------|--------|
| وجود `google-services.json` في `src/dev` و`src/staging` و`src/prod` | **محقَّق** (بعد تصحيح `prod`) |
| تطابق Application ID / `package_name` | **محقَّق** |
| استقلال Project ID/Number للثلاث بيئات | **محقَّق** |
| وجود SA محلياً خارج المستودع وتطابق `project_id` | **محقَّق** |
| عدم تتبع SA / عدم وجودها في سجل Git | **محقَّق** |
| توثيق `GOOGLE_APPLICATION_CREDENTIALS` | **محقَّق** (`FCM-ENVIRONMENTS.md`) |
| توثيق عزل البيئات | **محقَّق** |
| توثيق تأجيل iOS/APNs | **محقَّق** |
| عدم تفعيل الإرسال / عدم تثبيت SDK / عدم تشغيل جهاز | **محقَّق** (لم تُنتهك القيود) |

---

## 7) الحكم

**READY TO CLOSE** لمهمة **FCM-C-01** بعد موافقة المالك الصريحة.

لا إغلاق تلقائي. لا بدء لـ FCM-C-02 حتى تفويض لاحق. العدّاد الرسمي للنظام (21/50) **بلا تغيير**.

---

## 8) معلومة هندسية

في Android Gradle، اسم مجلد الـ flavor source set يجب أن يطابق اسم الـ `productFlavor` حرفياً. وضع `google-services.json` تحت `src/production/` بينما الـ flavor اسمه `prod` يعني أن ملف الإنتاج **لن يُدمَج** في بناء `prod` رغم صحة محتواه. التحقق من المسار الاسمي للـ flavor جزء من قبول تهيئة Firebase وليس تفصيلاً تجميلياً ثانوياً.
