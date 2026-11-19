# تقرير FCM-C-07 — الاختبارات والتوثيق والقبول النهائي لمرحلة FCM-C

| الحقل | القيمة |
|--------|--------|
| المهمة | **FCM-C-07** |
| التاريخ | 2026-11-19 |
| الحالة | **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING** |
| النطاق | تجميع أدلة Backend + Mobile وتمييز المثبت عن المؤجَّل |

---

## 1) Backend Verification — نتائج فعلية

| الفحص | النتيجة |
|-------|---------|
| `dotnet build Backend.sln -c Release` | **Build succeeded — 0 Warning(s), 0 Error(s)** |
| `dotnet test Application.Tests -c Release` | **Passed: 114 / Failed: 0 / Skipped: 0 / Total: 114** |
| `dotnet test API.IntegrationTests -c Release` | **Passed: 161 / Failed: 0 / Skipped: 21 / Total: 182** |
| اختبارات FCM Provider (`FcmProviderRegistrationTests`, `FakeFcmProviderTests`) | ضمن إجمالي `Application.Tests` أعلاه — لا فشل |
| اختبارات Push Tokens (`PushToken_Register_Update_Revoke_Is_Owner_Scoped` وغيرها) | ضمن إجمالي `API.IntegrationTests` أعلاه — لا فشل |
| اختبارات Notification Worker (`FcmNotificationDispatchTests` — 10 حالات: Accepted/Retry-then-Fail/Multi-Token-Revoke/Ownership-Transfer×3/No-Active-Token/Corrupt-Ciphertext/Fake-MessageId-Prefix) | ضمن إجمالي `API.IntegrationTests` أعلاه — لا فشل |
| اختبارات الأمان/التفويض المرتبطة (ownership-scoped push tokens، 403/404 عبر-المستخدمين) | ضمن إجمالي `API.IntegrationTests` أعلاه — لا فشل |
| Regression Tests | **الإجمالي نفسه هو اختبار الانحدار** — لا فرق في العدد عن تشغيل FCM-C-02 (161/0/21) سوى إصلاح FCM-C-06 المضاف |

### مقارنة بالمراحل السابقة

| المرحلة | Application.Tests | API.IntegrationTests |
|---------|---------------------|------------------------|
| FCM-C-02 (قبل الإغلاق) | 114/0/0 | 161/0/21 (182) |
| FCM-C-07 (الآن، بعد إصلاح FCM-C-06) | **114/0/0 — مطابق** | **161/0/21 (182) — مطابق تماماً** |

**لا تراجع في أي عدد.** الـ 21 Skip لنفس الأسباب الموثَّقة في تقرير FCM-C-02 التكميلي (Docker/Mosquitto حي غير متاح في الجلسة) — لا علاقة بـ FCM.

## 2) Mobile Verification — نتائج فعلية

| الفحص | النتيجة |
|-------|---------|
| `flutter analyze` | **No issues found!** |
| `flutter test` (كامل المجلد) | **Passed: 76 / Failed: 0** |
| اختبارات Firebase Messaging (`firebase_fcm_messaging_test.dart`) | 18 حالة — ضمن الإجمالي |
| اختبارات Token Lifecycle (`push_token_lifecycle_test.dart`, `push_tokens_repository_impl_test.dart`) | 7 حالات — ضمن الإجمالي |
| اختبارات Notification Handling (`notification_open_coordinator_test.dart`, `foreground_alert_coordinator_test.dart`) | 4 حالات — ضمن الإجمالي |
| اختبارات التنقل عند النقر | ضمن `open_notification_usecase_test.dart` + `notification_open_coordinator_test.dart` |
| اختبارات عزل البيئات | `firebase_bootstrap_test.dart` (المنصّة الحالية = Windows، غير مدعومة بتصميم — يثبت سلوك البوابة لا نجاحاً مزيَّفاً) |
| اختبارات Shared Core (`smart_security_app_core`) | **لم تتأثر** — لم يُعدَّل أي ملف في `packages/smart_security_app_core` خلال كامل مرحلة FCM-C؛ لا حاجة لإعادة تشغيلها هنا |
| بناء APK dev | **لم يكتمل** — حاولت مرتين (التفاصيل في تقرير FCM-C-03 §7-8)؛ أُوقف الأخير بطلب صريح من المالك بسبب شبكة ضعيفة أثناء الجلسة |
| بناء APK staging | **لم يُحاوَل** (معلَّق على نتيجة dev) |
| بناء APK prod | **لم يُحاوَل** |

### مقارنة بالمراحل السابقة (كل رقم من تشغيل `flutter test` **الكامل** لمجلد `test/`، لا جزئي)

| المرحلة | عدد اختبارات Mobile (تراكمي) | analyze |
|---------|-------------------------------|---------|
| أول تشغيل كامل رُصِد فعلياً، بعد تغييرات FCM-C-03 | 63 | 0 مشاكل |
| بعد FCM-C-04 | 70 (+7) | 0 مشاكل |
| بعد FCM-C-05 | 76 (+6) | 0 مشاكل |
| **FCM-C-07 (الآن، بعد FCM-C-06 — لم يُعدَّل أي ملف Mobile فيها)** | **76 (بلا تغيير)** | **0 مشاكل** |

لا قياس أساس (baseline) حقيقي قبل بدء مرحلة FCM-C ذاتها كان مرصوداً بتشغيل كامل منفصل — أول تشغيل كامل في هذه الجلسة جاء بعد كتابة ملفات FCM-C-03 مباشرة. كل الزيادات اللاحقة (63→70→76) **تراكمية بالإضافة فقط**، ولم يُحذف أو يُعطَّل أي اختبار سابق في أي خطوة.

## 3) Integration Verification — تمييز صريح بين الشامل والمنفصل

لا يوجد اختبار واحد يمتد فعلياً عبر عمليتين (Backend .NET + Mobile Dart) في نفس التشغيل — هذا غير ممكن تقنياً بأدوات الاختبار المستخدَمة دون بنية تحتية إضافية (لم تُطلَب). الرحلة الموصوفة في خطوات المهمة (١-١٠) مُغطَّاة فعلياً عبر **مكوّنين منفصلين متطابقين في المرجع**:

| خطوات الرحلة | الجهة | الدليل |
|---------------|-------|--------|
| 1-2: إنشاء حدث اختبار + حفظ الحادثة | Backend | `DatabaseScenarioSeed` + اختبارات `FcmNotificationDispatchTests` (تُنشئ Alarm/Notification فعلياً في PostgreSQL اختباري) |
| 3-6: سجل الإشعار + تحديد المستحق + Tokens | Backend | نفس الملف — `PushToken_Register_Update_Revoke_Is_Owner_Scoped`, `Multi_Token_Creates_Delivery_Per_Token_And_Revokes_Invalid` |
| 7-8: معالجة العامل + Fake Provider + بناء الحمولة | Backend | `Dispatch_Accepted_Writes_Deliveries...`, `Fake_Accepted_MessageIds_Are_Prefixed_Not_Firebase_Evidence` — يستخدم `FcmPayloadBuilder` الفعلي |
| 9: معالجة الحمولة داخل Flutter بـ Test Doubles | Mobile | `firebase_fcm_messaging_test.dart` (fakes لبوابة SDK) + `notification_open_coordinator_test.dart`/`foreground_alert_coordinator_test.dart` (fakes للمنفذ والمستودع) — تُعالج **نفس بنية الحمولة** (`notificationId`/`notificationType`/`deviceId`/`alarmEventId`) التي يبنيها `FcmPayloadBuilder` فعلياً |
| 10: فتح التفاصيل وفق الصلاحيات | كلاهما | Backend: `InFlight_Provider_Accept_Cannot_Be_Unsent_But_Detail_Denies_Prior_Owner` (يثبت 403/404 خادمياً) · Mobile: `open_notification_usecase_test.dart` (يثبت أن العميل يحترم الرفض دون افتراض محلي) |

**لا ادّعاء بنجاح سلسلة حية واحدة متصلة** — كل الأدلة أعلاه عبر Fake/Test Doubles بالتصميم (ممنوع إرسال حي ضمن القيود)، وهذا موثَّق بوضوح في كل تقرير فرعي.

## 4) مراجعة عدم التراجع الوظيفي

| المجال | الحالة |
|--------|--------|
| Authentication / Authorization | **لا تراجع** — `auth_session_controller_test.dart`, `auth_guard_test.dart`, `auth_interceptor_test.dart` كلها ناجحة؛ لم يُعدَّل أي من هذه الملفات |
| إدارة الأجهزة / الملكية ونقل الملكية | **لا تراجع** — اختبارات `devices/*` كلها ناجحة؛ Backend `Ownership_Transfer_During_Queue_...`/`After_Transfer_New_Alarm_...` ناجحة |
| إدارة الحوادث | **لا تراجع** — لم يُلمَس `AlarmEventIngestService`/مسار الحفظ |
| تسجيل الإشعارات / صندوق الإشعارات | **تحسين موثَّق لا تراجع** — `NotificationsListViewModel`/`NotificationsPage` لم يُعدَّلا؛ `ForegroundAlertCoordinator` الجديد يستدعي `.load()` الموجودة فقط |
| دورة حياة جلسة المستخدم | **لا تراجع** — `AuthSessionController` لم يُعدَّل؛ `PushTokenLifecycle` يستهلك حالتها فقط (قراءة) |
| التوجيه داخل التطبيق | **لا تراجع** — `app_router.dart`: إضافة `navigatorKey` فقط، لا تعديل في أي `GoRoute` |
| Shared Core | **غير متأثر** (§2) |
| قواعد البيانات | **لا تغيير** — لا Migration جديدة في كامل مرحلة FCM-C |
| وظائف MQTT الحالية | **لا تراجع** — اختبارات MQTT (Isolated/Live) بنفس حالة التجاوز السابقة تماماً (21 Skip مطابقة) |
| وظائف Web الحالية | **خارج النطاق، لم تُلمَس** (`admin_web` لم يُفتَح في كامل هذه المرحلة) |
| الحماية المحلية (ESP) | **خارج النطاق، لم تُلمَس** (`security_esp` لم يُفتَح) |

## 5) ما تعذّر إكماله (مرجَعاً لـ `fcm-remaining-tasks.md`)

انظر `docs/fcm/fcm-remaining-tasks.md` للقائمة الكاملة المُحدَّثة (FCM-R-01…R-09). أهمها: بناء APK لكل flavor (شبكة)، توكن/جهاز حقيقي لكل من C-04/C-05 (قيد صريح)، تأكيد يدوي لتفعيل FCM API في Console (403 آلي سابق)، iOS بالكامل (مؤجَّل من قبل هذه المرحلة).

## 6) الحكم

**SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING.** كل الأعمال البرمجية الممكنة دون جهاز/شبكة مستقرة/تفويض IAM اكتملت واختُبرت فعلياً بأدلة قابلة لإعادة الإنتاج (أعداد اختبارات محدَّدة، لا نسب تقديرية). عدّاد مرحلة FCM المستقل يصبح بعد هذا التقرير **6/7 منطقياً من ناحية التنفيذ البرمجي** (C-01…C-06 مكتملة برمجياً) — **لكن لا تُعلَّم أي بطاقة `[x]` ولا يتغيّر العدّاد الرسمي المُتعقَّب في `fcm-completion-tasks.md` إلا بموافقتك الصريحة على كل بطاقة**، وفق قاعدة الإغلاق المعلنة في بداية هذه المرحلة.
