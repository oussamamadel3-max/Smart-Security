# تقرير FCM-C-02 — تفعيل FirebaseFcmProvider (Backend / dev)

| الحقل | القيمة |
|--------|--------|
| المهمة | **FCM-C-02** |
| التاريخ | 2026-11-19 |
| الحالة | **READY TO CLOSE** — بانتظار `APPROVE TASK CLOSURE: FCM-C-02` |
| النطاق | Backend فقط · بيئة **dev** · مشروع `smart-security-dev-28642` |
| لم يُنفَّذ | FCM-C-03… · Flutter/Web/ESP · staging/prod · إرسال/استقبال إشعار حي · Commit/Push |

مراجع: `BE-T-09` · `docs/fcm/FCM-ENVIRONMENTS.md` · `docs/fcm/fcm-completion-tasks.md` · إغلاق FCM-C-01

---

## 1) قبل / بعد

| البند | قبل | بعد |
|-------|-----|-----|
| `appsettings.json` المتتبَّع | `Enabled=false` · `Provider=Fake` | **نفس القيم** (+ `FirebaseProjectId=""`) |
| تسجيل المزوّد | Firebase يرفض بلا ADC | + فرض `FirebaseProjectId` + تطابق `project_id` لملف SA |
| `FirebaseFcmProvider` | ADC بدون ProjectId صريح | يمرّر `ProjectId` من الخيارات |
| User Secrets | بلا `Fcm:TokenEncryptionKey` | المفتاح **مضبوط** (قيمة غير معروضة) |
| جلسة dev محلية | غير مفعّلة | إقلاع ناجح مع `Provider=Firebase` و`Enabled=true` عبر env |
| اختبارات آلية | Fake | ما زالت Fake (161 تكامل + 114 وحدة) |

---

## 2) التعديلات والملفات

| مسار | التغيير |
|------|---------|
| `Application/Options/FcmOptions.cs` | خاصية `FirebaseProjectId` |
| `Infrastructure/InfrastructureServiceRegistration.cs` | حراسة ProjectId + تطابق ملف الاعتماد · لا سقوط صامت لـ Fake |
| `Infrastructure/Services/Fcm/FirebaseFcmProvider.cs` | حقن `IOptions` · تعيين `AppOptions.ProjectId` |
| `Application.Tests/FcmProviderRegistrationTests.cs` | 4 اختبارات تسجيل (Fake / بلا ADC / بلا ProjectId / mismatch) |
| `API/appsettings.json` | إضافة `FirebaseProjectId: ""` فقط · **بدون** تفعيل |
| `docs/fcm/FCM-ENVIRONMENTS.md` | تشغيل/إيقاف جلسة dev |
| `docs/fcm/fcm-completion-tasks.md` | حالة C-02 |
| User Secrets (`esp32-security-backend-api`) | `Fcm:TokenEncryptionKey` (خارج Git) |
| خارج المستودع: `...\firebase\dev\run-backend-fcm-dev.ps1` | سكربت تشغيل محلي |

**لم يُعد بناؤه:** `FakeFcmProvider` · عامل الإرسال · عقود API · مخطط DB · Flutter.

---

## 3) آلية المصادقة والتهيئة

1. `GOOGLE_APPLICATION_CREDENTIALS` → SA لمشروع `smart-security-dev-28642` خارج Git.
2. عند `Fcm:Provider=Firebase`: التحقق من `FirebaseProjectId` ثم قراءة `project_id` من ملف الاعتماد (دون تسجيل الأسرار) ثم ADC.
3. `FirebaseApp.Create` عبر `GoogleCredential.GetApplicationDefault()` مع `ProjectId` المتوقَّع.
4. العامل `FcmNotificationHostedService` يعمل فقط إذا `Fcm:Enabled=true` في الجلسة.
5. العودة إلى Fake: إيقاف العملية ومسح متغيرات الجلسة؛ الملفات المتتبَّعة تبقى Fake.

---

## 4) نتائج الاختبارات

| الأمر / الفحص | النتيجة |
|---------------|---------|
| `dotnet build Backend.sln -c Release` | **نجح** · 0 Warning · 0 Error |
| `dotnet test Application.Tests` | **Passed 114 / Failed 0 / Skipped 0** |
| `FcmProviderRegistrationTests` (مصفّى) | **4 / 4 Passed** |
| `dotnet test API.IntegrationTests` | **Passed 161 / Failed 0 / Skipped 21** · Total 182 |
| Google ADC auth (`firebase.messaging` scope) | **GOOGLE_AUTH=OK** (طول الرمز مُبلغ دون طباعة القيمة) |
| `FirebaseApp.Create` لمشروع dev | **FIREBASE_APP_INIT=OK** |
| إقلاع API `:5088` + `/health` | **health=OK** · سجل: `Fcm:Enabled=true` · `provider Firebase` |
| تسريب `private_key` في سجلات الإقلاع | **SECRET_LEAK=NO** |
| `appsettings.json` بعد التشغيل | ما زال `Enabled=false` / `Provider=Fake` |

ملاحظة: اختبارات Fake في التكامل مثبتة عبر `AuthApiFactory` (`Fcm:Provider=Fake`, `Enabled=false`) — لا تتصل بـ Firebase الحي.

---

## 5) أدلة الإقلاع والمصادقة

- جلسة: `ASPNETCORE_ENVIRONMENT=Development` + ADC dev + `Fcm__Provider=Firebase` + `Fcm__FirebaseProjectId=smart-security-dev-28642` + `Fcm__Enabled=true`.
- سطر السجل (آمن):  
  `Fcm:Enabled=true. Worker will poll notification queue using provider Firebase. ACCEPTED means provider acceptance only — not device receipt or user-read.`
- `Now listening on: http://127.0.0.1:5088` · `Hosting environment: Development`.
- تهيئة `FirebaseApp` داخل المزوّد **كسولة** حتى أول `SendAsync`؛ أُثبتت التهيئة بشكل منفصل عبر `FirebaseApp.Create` بنفس ADC/ProjectId.

---

## 6) ما أُثبت / ما لم يُثبت

| البند | الحكم |
|-------|--------|
| تسجيل Firebase مع ADC صالح (dev) | **VERIFIED** |
| رفض غياب ADC / غياب ProjectId / mismatch مشروع | **VERIFIED** (وحدة) |
| عدم التحول الصامت إلى Fake | **VERIFIED** |
| إبقاء Fake في Git والاختبارات الآلية | **VERIFIED** |
| مصادقة Google لصلاحية `firebase.messaging` | **VERIFIED** (بدون طباعة token) |
| تفعيل FCM HTTP v1 في Console كقدرة منتج | **PARTIAL** — نجاح نطاق OAuth ودعم Admin SDK؛ لم يُنفَّذ Send حي |
| تسليم إشعار لجهاز / token حقيقي | **NOT VERIFIED** (مؤجّل عمداً — لا اختلاق token) |
| staging / prod | **غير مفعّل** |
| قاعدة بيانات تطوير «معزولة باسم مستقل» | **VERIFIED** بعد الاستكمال — `esp32_security_fcm_dev` + Migrations · انظر التقرير التكميلي |
| تفعيل FCM API / أدوار IAM من Console | **PARTIAL** — OAuth و`FirebaseApp` OK؛ Service Usage/IAM من SA → 403 · خطوات يدوية للمالك |

**تقرير تكميلي ما قبل الإغلاق:** [`FCM-C-02-supplement-report.md`](./FCM-C-02-supplement-report.md)

---

## 7) أعمال مؤجّلة

- FCM-C-03…C-07 (Flutter SDK، دورة token، fg/bg، أمان إرسال حي، قبول نهائي).
- إرسال/استقبال رسالة حقيقية بجهاز.
- تفعيل staging/prod.
- تأكيد يدوي في Google Cloud Console أن **Firebase Cloud Messaging API** مفعّل على `smart-security-dev-28642` (تعذّر التحقق الآلي: 403 من SA).

---

## 8) شروط القبول

| الشرط | الحالة |
|-------|--------|
| استكمال نواقص التفعيل دون إعادة بناء Fake/العامل | **محقَّق** |
| استخدام مشروع/SA لـ dev فقط | **محقَّق** |
| ADC مضبوط خارج المستودع | **محقَّق** |
| تطابق project_id ومنع الخلط | **محقَّق** |
| TokenEncryptionKey عبر User Secrets | **محقَّق** |
| تفعيل الجلسة المحلية Enabled+Firebase | **محقَّق** |
| الإبقاء على Fake في Git | **محقَّق** |
| الاختبارات الآلية على Fake | **محقَّق** |
| إقلاع Backend بالمزوّد الحقيقي | **محقَّق** |
| مصادقة Google بلا تسريب أسرار | **محقَّق** |
| رفض اعتمادات مفقودة/غير متطابقة | **محقَّق** |
| توثيق التشغيل/الإيقاف | **محقَّق** |
| عدم اختلاق token / عدم ادّعاء تسليم | **محقَّق** |
| قاعدة معزولة `esp32_security_fcm_dev` + سكربت محدّث | **محقَّق** (تكميلي) |
| عدم إرسال غير مقصود في الجلسة على `esp32_security` | **محقَّق** (تكميلي — 0 writes) |

---

## 9) الحكم

**READY TO CLOSE** لمهمة **FCM-C-02** بعد موافقة المالك الصريحة (يشمل استكمال العزل في التقرير التكميلي).

عدّاد مرحلة FCM يبقى **1/7** حتى الإغلاق. عدّاد النظام الرسمي **21/50 بلا تغيير**. لا بدء لـ FCM-C-03 قبل الموافقة.

---

## 10) معلومة هندسية

`dotnet run --no-launch-profile` بدون `ASPNETCORE_ENVIRONMENT=Development` لا يحمّل User Secrets، فيفشل الإقلاع على `Auth:Jwt:SigningKey` قبل الوصول لمسار FCM. تفعيل Firebase المحلي يجب أن يجمع صراحةً: بيئة Development + ADC + `Fcm__FirebaseProjectId` المطابق + **اتصال صريح بقاعدة `esp32_security_fcm_dev`** — وإلا يُخلط تشخيص «فشل Firebase» مع فشل أسرار التطبيق أو يُعرَّض طابور القاعدة العامة لعامل إرسال حي.
