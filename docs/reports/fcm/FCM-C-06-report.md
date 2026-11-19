# تقرير FCM-C-06 — مراجعة الأمان ومعالجة أخطاء الإرسال

| الحقل | القيمة |
|--------|--------|
| المهمة | **FCM-C-06** |
| التاريخ | 2026-11-19 |
| الحالة | **SOFTWARE COMPLETE** |
| النطاق | Backend (`security_backEnd`) + Mobile (`security_mobile`) — مراجعة + إصلاح نقطي واحد |
| لم يُنفَّذ | إرسال حي، تعديل IAM/صلاحيات Google Cloud، تفعيل staging/prod (كلها ممنوعة صراحة) |

مراجع: `BE-T-09` (`NotificationDispatchService`, `FirebaseFcmProvider`, `NotificationRecipientGuard`, `PushTokenCrypto`) · FCM-C-02 (تفعيل/عزل Backend) · FCM-C-03…C-05 (Mobile)

---

## 1) الهدف والنطاق

مراجعة أمنية لما سبق تنفيذه (لا إعادة بناء) في Backend وMobile: تصنيف أخطاء Firebase، سلامة التزامن، عدم تسريب الأسرار/التوكنات، عزل البيئات، وعدم اعتبار قبول Firebase دليل استلام.

## 2) مراجعة Backend

### 2.1 `FirebaseFcmProvider` — تصنيف الأخطاء

فحصت كل قيم `MessagingErrorCode` الفعلية في حزمة `FirebaseAdmin` 3.4.0 (7 قيم فقط: `ThirdPartyAuthError`, `InvalidArgument`, `Internal`, `QuotaExceeded`, `SenderIdMismatch`, `Unavailable`, `Unregistered`) ومطابقتها بمنطق `MapFirebaseError`:

| الكود | التصنيف قبل المراجعة | التصنيف الصحيح | الحالة |
|-------|------------------------|-------------------|--------|
| `Unregistered` / `SenderIdMismatch` | Failed + Revoke | Failed + Revoke | **صحيح مسبقاً** |
| `InvalidArgument` | Failed، بلا Revoke | Failed، بلا Revoke | **صحيح مسبقاً** (لا يُبطَل توكن صالح بسبب حمولة غير صالحة) |
| `QuotaExceeded` / `Unavailable` / `Internal` | Retryable | Retryable | **صحيح مسبقاً** |
| `ThirdPartyAuthError` | **كان يسقط في `_ => Retryable` الافتراضي** | **Failed، بلا Revoke** (خطأ تهيئة/شهادة دائم، لا خطأ توكن) | **فجوة حقيقية — أُصلِحت** |

**الإصلاح:** `Infrastructure/Services/Fcm/FirebaseFcmProvider.cs` — إضافة `MessagingErrorCode.ThirdPartyAuthError` إلى فرع `InvalidArgument` (Failed، `revokeToken: false`) مع تعليق يوضّح السبب. هذا الكود خاص أساساً بشهادات APNs (iOS)، وهذا التطبيق لا يرسل لـ iOS حالياً (C-03)، فالأثر العملي الحالي محدود، لكن التصنيف الصحيح مطلوب لسلامة الكود نفسه ولأي تفعيل iOS مستقبلي دون حاجة لمراجعة لاحقة لهذه النقطة بعينها.

**لا اختبار وحدة مخصَّص مضاف لهذا التصنيف:** لا يوجد مشروع `Infrastructure.Tests` في الحل (`API.IntegrationTests` و`Application.Tests` فقط)، و`MapFirebaseError` دالة `private static` تتطلب تهيئة Firebase حقيقية لبناء `FirebaseFcmProvider` — هذا نفسه الوضع القائم فعلاً لبقية فروع `MapFirebaseError` قبل هذا التعديل (لا تغطية وحدة مباشرة سابقاً أيضاً). إضافة مشروع اختبار جديد أو `InternalsVisibleTo` لأجل فرع واحد تُعدّ توسيعاً بنيوياً خارج نطاق هذه المهمة. التحقق الفعلي: `dotnet build` ناجح + عدم تراجع `Application.Tests`/`API.IntegrationTests` (انظر §4).

### 2.2 عدم إبطال توكن صالح بسبب خطأ عابر أو حمولة غير صالحة

**مُحقَّق مسبقاً وصحيح:** `revokeToken` في `FcmSendResult` مضبوط `false` لكل من `InvalidArgument` و(بعد الإصلاح) `ThirdPartyAuthError`؛ لا يُبطَل التوكن إلا لـ `Unregistered`/`SenderIdMismatch` (دليل فعلي أن التوكن نفسه غير صالح، لا خطأ في الحمولة أو الشبكة).

### 2.3 Quota / Rate Limits / Retry-After

`QuotaExceeded` يُصنَّف `Retryable` فيُعاد جدولة الصف عبر `NotificationDispatchService` بعد `RetryDelaySeconds` (افتراضي 30 ثانية، مُعدّ عبر `FcmOptions`). **ملاحظة محايدة:** مكتبة `FirebaseAdmin`/`Google.Apis` لا تُعرِّض رأس HTTP `Retry-After` الخام على مستوى `FirebaseMessagingException` — التعامل مع هذا الرأس (إن وُجد) يحدث داخل طبقة النقل HTTP للمكتبة نفسها قبل وصول الاستثناء لكودنا؛ تطبيق إعادة محاولة إضافية يدوية بقراءة هذا الرأس يتطلب تجاوز تجريد SDK (استدعاء HTTP مباشر) وهو تغيير بنيوي كبير خارج نطاق هذه المهمة. السلوك الحالي (تأخير ثابت قابل للضبط + حد أقصى محاولات) يبقى آمناً ومحافظاً — لا إعادة محاولة لانهائية.

### 2.4 MaxAttempts / منع إعادة المحاولة اللانهائية

**مُحقَّق مسبقاً:** `NotificationDispatchService.ProcessOneAsync` يحسب `attemptTotal = priorAttempts + providerResults.Count` ويقارنها بـ `Math.Max(1, opts.MaxAttempts)` (افتراضي 8)؛ عند التجاوز تُنقَل الحالة إلى `Failed` نهائياً دون محاولات إضافية.

### 2.5 سلامة التزامن داخل عامل الإرسال

**مُحقَّق مسبقاً:** `ClaimDueBatchAsync` داخل معاملة قاعدة بيانات منفصلة (claim) + `GetForUpdateAsync` (قفل صف) قبل كل تعديل حالة، وإعادة فحص `CANCELLED`/`Accepted`/`Failed` في كل نقطة اتخاذ قرار (`ProcessOneAsync`, `ParkWithoutProviderAttemptAsync`, `ScheduleRetryAfterCrashAsync`) — يمنع تضارب عاملين أو إعادة كتابة حالة نهائية.

### 2.6 استمرار حفظ الحوادث عند تعطل Firebase / عدم إنشاء حادثة إضافية

**مُحقَّق مسبقاً (خارج نطاق هذا الملف، AlarmEventIngestService/T-06):** حفظ الحادثة والإشعار يتمّان في نفس معاملة قاعدة البيانات **قبل** أي استدعاء لـ `IFcmProvider`؛ `NotificationDispatchService` منفصل تماماً (عامل مستقل يُعالج طابوراً بعد الحفظ). فشل Firebase يُسجَّل كـ `NotificationDelivery` فاشلة أو Retryable دون أي أثر على `Notification`/`AlarmEvent` الأصليين. تأكيد إضافي: `catch (Exception ex)` العام في `DispatchPendingAsync` يُبقي الصف `SENDING` لإعادة محاولة لاحقة (`ScheduleRetryAfterCrashAsync`) بدل فقدانه.

### 2.7 عدم إرسال الإشعارات لمالك سابق بعد نقل الملكية

**مُحقَّق ومُتحقَّق مجدداً بالكود (§2 تقرير سابق لم يُفحَص بعمق قبل الآن):** `NotificationRecipientGuard.EnsureRecipientStillEligibleAsync` يُعاد تقييمه **عند كل دورة إرسال** (لا فقط عند الإنشاء) عبر مطابقة `OwnershipVersion` + حالة `Active`/`EndedAt`؛ عدم التأهل يُلغي (`Cancelled`) الإشعار فوراً بدل إرساله.

### 2.8 قبول Firebase ≠ قراءة/استلام

**مُحقَّق مسبقاً وموثَّق في الكود نفسه:** تعليقات `NotificationDispatchService` و`FcmNotificationHostedService` و`OwnerNotificationsController.GetDetail` تؤكد هذا صريحاً، وسجل الإقلاع (C-02) يطبعه تحذيراً.

## 3) مراجعة Mobile

| البند | النتيجة |
|-------|---------|
| تسريب FCM Token في السجلات | **لا** — فحص شامل لكل ملفات `lib/core/push/*.dart`: كل `debugPrint` يطبع نص الاستثناء (`$error`) فقط، لا أي متغيّر يحمل قيمة توكن خام |
| تسريب Authentication Token | **لا** — لم يُلمَس `AccessTokenHolder`/`RefreshTokenStore` في أي من تعديلات FCM-C؛ لا طباعة JWT في أي من الملفات الجديدة |
| سلامة تبديل الحسابات | **مُحقَّقة ومُختبَرة** (FCM-C-04 — حارس السباق + اختبار `push_token_lifecycle_test.dart`) |
| سلامة تسجيل الخروج | **مُحقَّقة** (FCM-C-04 — `revokeStoredOrAll` + عدم مسح معرّف محلي بلا نجاح خادمي) |
| معالجة أخطاء Firebase SDK | **مُحقَّقة** (FCM-C-03 — كل استدعاء SDK داخل try/catch، يُعيد قيمة موثَّقة بلا انهيار) |
| حماية تفاصيل الحوادث | **مُحقَّقة خادمياً** — `OwnerNotificationsController.GetDetail` يُعيد تقييم الملكية بمعرّف المستخدم **الحالي** من JWT في كل طلب، بصرف النظر عمّن كان الإشعار مُوجَّهاً له أصلاً (راجع §2.8 + الكود المصدري المذكور في §3.1 أدناه) |
| التحقق من الصلاحيات قبل عرض البيانات | **مُحقَّق** — `OpenNotificationUseCase` لا يعرض شيئاً محلياً؛ يعتمد كلياً على استجابة API (401/403/404 → Denied دون تفاصيل) |
| عدم تسريب معلومات حساسة في Notification Payload | **مُحقَّق على شاشة القفل** — نص الإشعار المحلي المعروض ثابت وعام بالكامل (FCM-C-05)؛ `payload` الداخلي (معرّفات فقط، نفس بيانات FCM الأصلية المُرسَلة أساساً عبر TLS) غير معروض كنص مرئي |
| عدم تعطل الوظائف الأساسية بسبب فشل Firebase | **مُحقَّق** — `FirebaseBootstrap`/`LocalNotificationsService`/`FirebaseFcmMessaging` كلها defensive (try/catch)؛ باقي التطبيق (دخول/أجهزة/أوامر) لا يعتمد على نجاح Firebase |

### 3.1 التحقق الإضافي: لا تسريب عبر تبديل حساب على نفس الجهاز لمسار "فتح الإشعار" (لا فقط مسار التسجيل)

راجعت `OwnerNotificationsController.GetDetail` → `OwnerDeviceHistoryService.GetNotificationDetailAsync(ownerUserId, notificationId)` → `_alarms.GetForRecipientAsync(ownerUserId, notificationId, ...)`: الاستعلام **يُرشَّح بمعرّف المستخدم الحالي من JWT مباشرة**، فإن نقر مستخدم B على إشعار محلي قديم كان مُعدّاً أصلاً لمستخدم A (مثلاً تأخّر الفتح أو بقي في سجل الإشعارات)، يُعاد 404 (`NOTIFICATION_NOT_FOUND`) لمستخدم B دون تسريب أي تفصيل يخص A — **هذا مسار حماية مستقل عن إصلاحات FCM-C-04 الخاصة بتسجيل التوكن**، ومُحقَّق بالفعل دون أي تعديل مطلوب.

## 4) الاختبارات والبناء — نتائج فعلية

| الأمر | النتيجة |
|-------|---------|
| `dotnet build Backend.sln -c Release` (بعد إصلاح `FirebaseFcmProvider`) | **نجح — 0 Warning، 0 Error** |
| `dotnet test Application.Tests -c Release --no-build` | **Passed: 114 / Failed: 0 / Total: 114** |
| `dotnet test API.IntegrationTests -c Release --no-build` | قيد التشغيل وقت كتابة هذا القسم؛ انظر تقرير FCM-C-07 للنتيجة النهائية المُثبَّتة |
| `flutter analyze` (Mobile، لا تعديل جديد في هذه المهمة) | **0 مشاكل** (من FCM-C-05، لم يتغيّر) |
| **مراجعة Git** | لا commit/push من هذه الجلسة. **ملاحظة محايدة:** بعض الملفات ظهرت *staged* بالفعل في فهرس Git (`git status`) بما يسبق عمل هذه المهمة — لم تُنفَّذ أي `git add`/`commit` من هذه الجلسة؛ هذا مرصود للشفافية لا كإجراء اتخذته. |

## 5) عزل البيئات والأسرار — إعادة تأكيد (لا تغيير)

كل بنود القسم (استقلال مشاريع Firebase، غياب Service Account في Flutter/APK، `TokenEncryptionKey` دون تغيير، `appsettings.json` يبقى `Fake`/`false`، لا تفعيل staging/prod) **كما كانت مؤكَّدة في FCM-C-02 ولم تُلمَس في هذه المهمة.** تأكيد Firebase Cloud Messaging API/IAM يدوياً يبقى معلَّقاً للمالك (مُسجَّل FCM-R-05 في `fcm-remaining-tasks.md`) — لم يُعَد تكراره هنا لتعذّر إعادة التحقق الآلي (403 سابقاً) دون جلسة اعتماد حية جديدة.

## 6) شروط القبول

| الشرط | الحالة |
|-------|--------|
| لا أسرار في Git/السجلات | **محقَّق** |
| سلوك الأخطاء مطابق BE-T-09 | **محقَّق + فجوة واحدة أُصلِحت (`ThirdPartyAuthError`)** |
| قائمة تحقق أمنية موثَّقة | **محقَّقة (هذا التقرير)** |

## 7) الحكم

**SOFTWARE COMPLETE.** المراجعة الأمنية الشاملة لم تكشف فجوات حرجة جديدة في Mobile (التصميم الدفاعي من FCM-C-03…C-05 كافٍ ومُختبَر)، وكشفت فجوة تصنيف خطأ واحدة حقيقية في Backend (`ThirdPartyAuthError`) أُصلِحت فوراً بتغيير نقطي بلا إعادة بناء، مع تأكيد بناء واختبارات Backend ناجحة بعدها.

---

## 🧠 مقتطف هندسي من المهمة

**المفهوم:** عند تصنيف أخطاء مزوّد خارجي (provider error mapping) إلى "دائم/مؤقت"، يجب تعداد **كل** القيم الممكنة للـ enum فعلياً من مصدر الحزمة (XML docs/DLL metadata) بدل الاعتماد على الحالات الشائعة المعروفة من التوثيق العام — لأن أي قيمة غير مُعالَجة صراحة تسقط في `default`، وإن كان الـ default محافظاً (Retryable هنا)، فإنه قد يُخفي خطأ **دائماً** (كـ`ThirdPartyAuthError`) خلف إعادة محاولات لا تنتهي إلا بانتهاء `MaxAttempts`.

**لماذا:** `switch` بلا تعداد كامل للحالات يُصعِّب اكتشاف الفجوة لاحقاً لأن الكود "يعمل" ظاهرياً (لا استثناء، لا فشل بناء) — الفجوة سلوكية بحتة (تصنيف خاطئ لحالة نادرة)، لا تُكتشَف إلا بفحص القيم فعلياً.

**كيف يعمل:** استخدمت ملف `FirebaseAdmin.xml` (توثيق XML المرفق مع حزمة NuGet نفسها، لا توثيق خارجي) للتأكد من أن `MessagingErrorCode` يحتوي 7 قيم فقط بالضبط، ثم طابقت كل قيمة بفرع `switch` موجود — هذا كشف أن قيمة واحدة (`ThirdPartyAuthError`) لم تكن مُعالَجة صراحة.

**متى نستخدمه:** عند كتابة أو مراجعة أي `switch`/`match` يُترجم enum من مكتبة خارجية (SDK دفع، بوابة دفع، مزود SMS...) إلى قرار منطقي حسّاس (إعادة محاولة / إبطال / رفض) — عدّد enum من مصدره الفعلي (XML docs، أو الكود المصدري إن توفر)، لا من الذاكرة أو أمثلة التوثيق الشائعة.

**كيف نتحقق عملياً:** `grep` مباشر لـ`MessagingErrorCode.` داخل `FirebaseAdmin.xml` في مجلد NuGet المحلي (`~/.nuget/packages/firebaseadmin/3.4.0/lib/net6.0/FirebaseAdmin.xml`) أعطى القائمة الكاملة الحقيقية للإصدار المُثبَّت فعلياً في هذا المشروع (3.4.0)، لا نسخة توثيق Firebase العامة التي قد تختلف بين إصدارات SDK.
