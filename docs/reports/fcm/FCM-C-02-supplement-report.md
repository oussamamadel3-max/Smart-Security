# تقرير تكميلي — FCM-C-02 (ما قبل الإغلاق)

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-19 |
| الغرض | استكمال ملاحظات المراجعة قبل `APPROVE TASK CLOSURE: FCM-C-02` |
| الحالة | **READY TO CLOSE** (بعد الاستكمال) · البطاقة ما زالت `[ ]` |
| عدّاد FCM | **1/7** بلا تغيير حتى الإغلاق |
| عدّاد النظام | **21/50** بلا تغيير |

---

## 1) عزل قاعدة البيانات ومراجعة الجلسة السابقة

### 1.1 جلسة `esp32_security` السابقة (قبل العزل)

من `fcm-c02-api-boot2.log`:

| الدليل | المعنى |
|--------|--------|
| اتصال `database 'esp32_security'` | الجلسة الأولى استخدمت القاعدة الأصلية |
| استعلام claim على `notifications` … `FOR UPDATE SKIP LOCKED` | العامل استطلع الطابور |
| `SaveChanges completed … with 0 entities written` | **لم يُحدَّث أي صف** (لا claim فعلي) |
| غياب `Firebase messaging failed` / message id | **لا محاولة إرسال** عبر Admin SDK |
| لا `private_key` / token خام في السجل | لا تسريب اعتمادات |

**الخلاصة:** لم يُرسل إشعار غير مقصود ولم تُعالَج عناصر طابور إلى حالة إرسال.

### 1.2 القاعدة المعزولة الجديدة

| بند | النتيجة |
|-----|---------|
| إنشاء `esp32_security_fcm_dev` | **VERIFIED** (`DB_CREATED`) |
| القاعدة الأصلية `esp32_security` ما زالت موجودة | **VERIFIED** |
| نسخ مستخدمين/Tokens | **لم يُنفَّذ** (قاعدة فارغة بعد Migrations فقط) |
| `dotnet ef database update` على المعزولة | **VERIFIED** — `InitialCreate` + `UsersFullName…` + `AuthSessions…` · `migrate_exit=0` |
| تعديل مخطط / ترحيل جديد | **لا** |
| سكربت التشغيل المحلي | يفرض `ConnectionStrings__DefaultConnection` → `Database=esp32_security_fcm_dev` |
| إقلاع API على المعزولة | **VERIFIED** — `/health` OK · السجل يظهر `esp32_security_fcm_dev` فقط · `ORIGINAL_DB_IN_LOG=NO` · claim `0 entities written` · `SEND_ATTEMPT=NO` |

مسار السكربت (خارج Git):  
`C:\Users\Osama\Desktop\Secrets\smartSecurity\firebase\dev\run-backend-fcm-dev.ps1`

---

## 2) صلاحيات Firebase (dev)

| فحص | نتيجة | تفسير |
|-----|--------|--------|
| OAuth لـ `firebase.messaging` / `cloud-platform` | **OK** (طول token فقط) | مصادقة Google ناجحة |
| `FirebaseApp.Create` لمشروع `smart-security-dev-28642` | **OK** | تهيئة Admin SDK |
| Service Usage `fcm.googleapis.com` | **HTTP 403 PERMISSION_DENIED** | SA لا يملك قراءة حالة تفعيل الخدمات |
| IAM `getIamPolicy` | **HTTP 403 PERMISSION_DENIED** | لا يمكن سرد أدوار المشروع من هذا الحساب |
| إرسال فعلي / dry-run بـ token جهاز | **لم يُنفَّذ** (قيد المالك) | نجاح OAuth ≠ إثبات صلاحية الإرسال |

### تمييز صريح

- **مُثبت:** المصادقة (ADC + OAuth) وتهيئة `FirebaseApp`.
- **غير مُثبت آلياً:** تفعيل «Firebase Cloud Messaging API» في Console، وأدوار IAM التي تسمح بـ `messages:send`.
- **لم يُعدَّل IAM** (يتطلب موافقة المالك).

### خطوات يدوية مطلوبة من المالك (Google Cloud / Firebase Console)

على مشروع **`smart-security-dev-28642`** فقط:

1. **APIs & Services → Enabled APIs**  
   تأكد أن **Firebase Cloud Messaging API** (`fcm.googleapis.com`) بحالة **Enabled**.  
   إن لم تكن: Enable API.
2. **IAM** لحساب `firebase-adminsdk-…@smart-security-dev-28642.iam.gserviceaccount.com`  
   الأدوار الشائعة الكافية لإرسال FCM عبر Admin SDK تشمل عادةً دوراً يسمح بـ Firebase Admin / Cloud Messaging (مثل الأدوار الافتراضية التي يمنحها Firebase لـ Admin SDK service account عند إنشائه من Console).  
   **لا تغيّر الأدوار** إن كان الإرسال سيعمل لاحقاً؛ راجع فقط. أي تعديل IAM يحتاج موافقة مستقلة.
3. بعد جاهزية جهاز/token حقيقي (FCM-C-03+): اختبر Send واحداً في بيئة `esp32_security_fcm_dev` فقط.

---

## 3) الاختبارات المتجاوزة وعدم التراجع

### 3.1 لماذا 21 Skip؟

كلها **`[SkippableFact]`** لمسارات MQTT/Mosquitto حية تعتمد Docker/وسيط غير متاح في الجلسة — **ليست من تغييرات FCM-C-02**:

| المجموعة | العدد (تقريبي) | السبب |
|----------|----------------|--------|
| `LiveMqttTlsReliabilityTests` | 7 | Docker/TLS Mosquitto غير جاهز |
| `LiveMqttInboundIntegrationTests` | 6 | Mosquitto حي غير جاهز |
| `IsolatedMosquittoAclTests` | 6 | حاوية ACL غير جاهزة |
| `LiveMqttBrokerIntegrationTests` | 1 | Broker حي |
| `IsolatedPostgresKillRecoveryTests` | 1 | سيناريو إيقاف Postgres معزول غير جاهز |

**لا علاقة بـ FCM / FirebaseProjectId / ADC.**

التكامل المنفَّذ: **161 Passed / 0 Failed / 21 Skipped**.

### 3.2 عدم تراجع وظيفي FCM

| دليل | نتيجة |
|------|--------|
| وحدة FCM registration / Fake / Payload (إعادة تشغيل مصفّاة) | انظر أمر التشغيل في الجلسة |
| `AuthApiFactory` يفرض `Fcm:Provider=Fake` | الاختبارات الآلية لا تلمس Firebase الحي |
| رفض خلط بيئات (dev SA vs staging project id) | `Firebase_Provider_With_Mismatched_Credential_ProjectId_Throws` **Passed** |
| السجلات | `PushTokenId` فقط عند الفشل · لا `RawToken` · لا محتويات SA |

---

## 4) شروط القبول بعد الاستكمال

| الشرط | الحالة |
|-------|--------|
| مراجعة عدم إرسال غير مقصود على الجلسة السابقة | **محقَّق** |
| قاعدة `esp32_security_fcm_dev` + Migrations | **محقَّق** |
| لا نسخ بيانات / لا مساس بالأصلية | **محقَّق** |
| سكربت التشغيل يستخدم القاعدة المعزولة | **محقَّق** |
| إقلاع على القاعدة الجديدة | **محقَّق** |
| تمييز OAuth عن صلاحية الإرسال | **محقَّق** (توثيق + 403 على Service Usage/IAM) |
| خطوات يدوية للمالك عند تعذر التحقق | **موثَّقة أعلاه** |
| توضيح الـ 21 Skip | **محقَّق** |
| عدم إغلاق المهمة / عدم تغيير النسب | **محقَّق** |

---

## 5) الحكم

التنفيذ البرمجي لـ **FCM-C-02** مع استكمال العزل والتوثيق **جاهز للإغلاق** بموافقة المالك.

ما يبقى خارج إثبات C-02 (ولا يمنع الإغلاق إذا قبل المالك):

- تأكيد يدوي في Console أن FCM API Enabled.
- إثبات `messages:send` بـ token جهاز حقيقي (مراحل لاحقة).

**لا إغلاق تلقائي.** بانتظار: `APPROVE TASK CLOSURE: FCM-C-02`.
