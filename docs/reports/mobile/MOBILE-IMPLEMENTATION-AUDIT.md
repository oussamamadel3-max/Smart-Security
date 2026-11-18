# تدقيق ما بعد التنفيذ — `security_mobile` (MOB-T-01…MOB-T-08)

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-17 |
| النوع | مراجعة وتدقيق — **بدون تعديل كود/حوكمة/حالات مهام** |
| نطاق المراجعة | مهام التطوير، التقارير، الكود، الاختبارات، `design/index.html`، عقود Backend، استثناء AGENTS/6A |
| قيود المالك لهذا التدقيق | لا جهاز/محاكٍ · لا iOS · لا اعتماد G0/G1 · لا تطوير ويب |

---

## 1) ملخص تنفيذي

التطبيق يملك **أساساً صلباً للجلسة والأجهزة والأوامر والسجل** مربوطاً بمسارات Backend الحقيقية عبر Dio، مع فصل واضح بين PENDING والحالة المؤكدة.  
في المقابل، ادّعاء «اكتمال التسلسل 100%» في ملفات التقدّم يصف **نطاقاً ذاتياً مؤجّلاً** وليس اكتمال شروط القبول الإنتاجية لكل بطاقة. أبرز الفجوات: **NoOp FCM**، **Unavailable Wi-Fi client**، **بطاقات ACK/G1 غير مغلقة**، **شاشات التصميم الناقصة** (dashboard / notifications / profile)، و**اختبارات قبول ميدانية غير منفَّذة**.

| الحكم الإجمالي المقترح | القيمة |
|--------------------------|--------|
| جاهزية نطاق العميل المؤجّل (حسب استثناء 6A) | ~**82%** |
| جاهزية قبول إنتاجي كامل لبطاقات T-01…T-08 | ~**61%** |
| حكم التدقيق | **PARTIALLY READY** — لا يُعاد تعليم المهام في هذا التقرير |

---

## 2) نتائج التحقق الحالي (أُعيد تشغيلها في التدقيق)

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` | **نظيف** — No issues found |
| `flutter test` | **34/34** PASSED |
| `flutter build apk --debug --flavor dev --target-platform android-arm64` | **نجاح** → `build/app/outputs/flutter-apk/app-dev-debug.apk` |
| تشغيل جهاز/محاكٍ | **NOT VERIFIED** (ممنوع في مهمة التدقيق) |
| بناء iOS | **NOT VERIFIED** (ممنوع) |
| اعتماد G0/G1 | **لم يُنفَّذ** (ممنوع) |

---

## 3) حالة كل مهمة — الحكم الفعلي للتدقيق

| المهمة | حالة المهام في الملف | حكم التدقيق | ملاحظة |
|--------|----------------------|-------------|--------|
| **MOB-T-01** | مغلقة بموافقة صريحة | **COMPLETE** (+ **DEFERRED BY OWNER** للجهاز/iOS) | scaffold + core + flavors + APK |
| **MOB-T-02** | مغلقة بموافقة صريحة | **COMPLETE** (+ **DEFERRED BY OWNER** للجهاز/iOS) | جلسة/Refresh/single-flight مثبتة اختباراً |
| **MOB-T-03** | مغلقة تلقائياً | **COMPLETE** (عميل) · **NOT VERIFIED** (حيّ/هاتفان) | قائمة + PENDING/ACTIVE + cache |
| **MOB-T-04** | مغلقة تلقائياً (عميل) | **PARTIALLY IMPLEMENTED** | UI أوامر ✓ · ACK/G1 **BLOCKED** · نقل ملكية حيّ **NOT VERIFIED** |
| **MOB-T-05** | مغلقة تلقائياً | **COMPLETE** (عميل أساسي) · فجوات طفيفة | سجل + بحث محلي؛ لا ترقيم خادم؛ ربط إشعار لاحق |
| **MOB-T-06** | مغلقة تلقائياً (عميل) | **PARTIALLY IMPLEMENTED** | HTTP push-tokens + open-by-id ✓ · **NoOpFcmMessaging** · Firebase **DEFERRED/BLOCKED** |
| **MOB-T-07** | مغلقة تلقائياً (هيكل) | **PARTIALLY IMPLEMENTED** | UI معزول ✓ · **UnavailableLocalProvisioningClient** · لا soft-AP فعلي |
| **MOB-T-08** | مغلقة تلقائياً (آلي) | **PARTIALLY IMPLEMENTED** | وحدة/smoke ✓ · ميداني **DEFERRED BY OWNER** |

---

## 4) مصفوفة تغطية المتطلبات

تصنيف كل صف: `COMPLETE` | `PARTIALLY IMPLEMENTED` | `NOT IMPLEMENTED` | `BLOCKED` | `NOT VERIFIED` | `DEFERRED BY OWNER`

### MOB-T-01

| المتطلب / البطاقة | الكود | الاختبار / الدليل | التصنيف |
|-------------------|-------|-------------------|---------|
| `smart_security_app_core` ثيم/أخطاء/HTTP أساسي/l10n | `packages/smart_security_app_core` | analyze/test الحزمة (تاريخي) + استهلاك path | COMPLETE |
| Android/iOS flavors + path | `android/.../build.gradle.kts`, `pubspec.yaml` | APK `dev` في التدقيق | COMPLETE |
| هيكل features + توجيه | `lib/core`, `lib/features`, `app_router.dart` | widget_test إقلاع→login | COMPLETE |
| بيئات بلا أسرار | `app_environment.dart` (`API_BASE_URL` / placeholders) | مراجعة كود | COMPLETE |
| إقلاع جهاز/محاكٍ | — | — | DEFERRED BY OWNER |
| بناء iOS | — | — | DEFERRED BY OWNER |

### MOB-T-02 — جلسة / شبكة / تدوير رموز

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| Access في الذاكرة فقط | `AccessTokenHolder`, `AuthSessionController` | `auth_session_controller_test` | COMPLETE |
| Refresh في secure storage + body | `SecureRefreshTokenStore`, `AuthRemoteDataSource.refresh` | session tests | COMPLETE |
| single-flight عند 401 متزامن | `AuthInterceptor` | `auth_interceptor_test` (concurrent 401) | COMPLETE |
| 401 ≠ 403 (لا logout على 403) | `AuthInterceptor.onError` | interceptor + auth_guard tests | COMPLETE |
| فشل تجديد → مسح حساس + دخول | `markSessionExpired`, `_hardClear` | session tests | COMPLETE |
| حراسة المسارات | `auth_guard.dart`, `GoRouter.redirect` | `auth_guard_test` | COMPLETE |
| enum مجهول ≠ نجاح | `device_status_codes.dart` | `device_status_codes_test` | COMPLETE |
| لا تسريب رموز في logs | لا `print` للرموز في auth/network (مراجعة) | مراجعة ثابتة | COMPLETE |
| جهاز/iOS حي | — | — | DEFERRED BY OWNER |

### MOB-T-03 — ملكية / مؤشرات / cache

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| قائمة أجهزة المالك فقط | `GET /api/owner/devices` عبر `DevicesRemoteDataSource` | mapper/entity tests | COMPLETE |
| PENDING محدود بلا تحكم/سجلات | `showsLimitedPendingView`, UI list/detail | `owner_device_entity_test` | COMPLETE |
| ACTIVE + مؤشرات مستقلة | `DeviceIndicatorsEntity`, pills | mapper + entity tests | COMPLETE |
| offline ≠ disarm/health | `showsStaleWarning` | entity test | COMPLETE |
| ACTIVE awaiting sync: طلب جائز / نشر محجوز خادماً | `canControl`, `awaitingOwnershipAck`, notices | entity test | COMPLETE (عرض عميل) |
| إفراغ cache عند 401/403/404/خروج/تبديل حساب | `DevicesRepositoryImpl` + `ref.listen(authSession)` | `devices_repository_cache_test` | COMPLETE |
| جهاز أجنبي لا يظهر | يعتمد عزل Backend + 404/403 مسح cache | وحدة جزئية | PARTIALLY IMPLEMENTED / NOT VERIFIED حيّ |
| هاتفان متسقان | — | — | NOT VERIFIED |

### MOB-T-04 — أوامر / G1 / ACK / مصالحة

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| أوامر ARM/DISARM/MUTE/TEST/STOP_TEST عبر HTTPS | `DeviceCommandsRemoteDataSource`, detail VM | entity tests + APK | COMPLETE (عميل) |
| HTTP 202/PENDING لا يقلب مؤشرات مؤكدة | `submitCommand` لا يحدّث indicators من الرد | `device_command_entity_test` | COMPLETE |
| لا `executeWithRetry` على POST | `DeviceCommandsRepositoryImpl.create` | مراجعة كود | COMPLETE |
| MUTE بلا `currentAlarmEventUid` ممنوع | VM + زر معطّل | مراجعة كود | COMPLETE |
| TEST أثناء ALARM ممنوع | VM + زر | مراجعة كود | COMPLETE |
| TIMED_OUT يحظر متعارضاً حتى مصالحة | `blocksConflictingUntilReconciled` | entity test | COMPLETE (منطق عميل) |
| polling ثم إعادة جلب الحالة | `_startPolling` + `load` | لا اختبار وحدة للـ poll | PARTIALLY IMPLEMENTED |
| EXECUTED مرتبط COMMAND_ACK كامل + ترتيب STATE | يعتمد G1/جهاز | — | BLOCKED |
| هاتفان / نقل ملكية متصل حيّ | — | بطاقة غير معلَّمة | NOT VERIFIED / BLOCKED |
| بطاقة اختبار ACK وفق G1 | غير معلَّمة في tasks | — | BLOCKED |

### MOB-T-05 — سجلات / بحث / عزل

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| قائمة حوادث ملكية حالية | `GET .../alarm-events` | mapper tests | COMPLETE |
| قائمة أوامر ملكية حالية | `GET .../commands` | datasource موجود | COMPLETE |
| تمييز إنذار vs اختبار | حوادث ≠ أوامر TEST في تبويبين | مراجعة UI | COMPLETE |
| wasBuffered بلا مدة مختلقة | `AlarmEventMapper` + detail notice | mapper test | COMPLETE |
| occurrence غير موثوق عند `clockTrusted=false` | mapper يسقط الوقت | mapper test | COMPLETE |
| بحث أساسي | فلتر نصي محلي في VM | لا اختبار مخصص | PARTIALLY IMPLEMENTED |
| ترقيم خادم | API بلا pagination؛ لا paging UI | — | NOT IMPLEMENTED (مقيّد بالعقد) |
| تفاصيل حادثة | `AlarmEventDetailPage` | — | COMPLETE |
| ربط بالإشعار لاحقاً | نص `notificationLinkLater` فقط | — | PARTIALLY IMPLEMENTED |
| عزل ملكية + إسقاط cache | repo drop على 401/403/404 | يعتمد خادم + وحدة جزئية | COMPLETE (عميل) / NOT VERIFIED حيّ |

### MOB-T-06 — FCM

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| HTTP register/revoke push-tokens | `PushTokensRemoteDataSource`, logout revoke | لا اختبار HTTP حي | PARTIALLY IMPLEMENTED |
| onTokenRefresh / إذن OS | `FcmMessagingPort` ← **NoOp** | — | NOT IMPLEMENTED (فعلي) |
| Firebase Apps + APNs لكل flavor | لا `firebase_*` في pubspec · لا google-services | — | BLOCKED / DEFERRED BY OWNER |
| فتح إشعار → API detail | `OpenNotificationUseCase` | `open_notification_usecase_test` | COMPLETE (مسار منطقي) |
| 403/404 بلا تفاصيل محلية | Denied بدون cache جسم | unit tests | COMPLETE |
| fg/bg/terminated حيّ | — | — | DEFERRED BY OWNER / NOT VERIFIED |
| رسالة اختبار على جهاز | — | — | DEFERRED BY OWNER |

> **ملاحظة تدقيق:** تعليم بطاقة FCM-03 `[x]` مع الاعتماد على `NoOpFcmMessaging` يعني أن **مسار HTTP جاهز لكن التسجيل الفعلي لا يحدث أبداً في الإنتاج الحالي** → لا يُعد اكتمالاً وظيفياً.

### MOB-T-07 — Wi-Fi محلي

| المتطلب / القبول | الكود | الاختبار | التصنيف |
|------------------|-------|----------|---------|
| مسار معزول عن ARM/DISARM | `WifiProvisioningPage` + notice | wifi tests | COMPLETE (فصل) |
| تعليمات حيازة + نموذج + إعادة محاولة | UI phases | VM test | COMPLETE (هيكل) |
| لا تخزين كلمة مرور | مسح password من state + `clearSensitive` | wifi tests | COMPLETE |
| إرسال محلي فعلي للجهاز | `UnavailableLocalProvisioningClient` → `unavailable` | wifi tests | NOT IMPLEMENTED |
| تأكيد اتصال من الجهاز | — | — | BLOCKED (ESP-T-08 / D-04) |
| استمرار إنذار محلي | — | — | NOT VERIFIED (عتاد) |

### MOB-T-08 — قبول

| المتطلب / البطاقة | الدليل | التصنيف |
|-------------------|--------|---------|
| اختبارات حرجة وحدة/smoke | 34 اختباراً بما فيها acceptance smoke | PARTIALLY IMPLEMENTED |
| قائمة تحقق SRS | `MOB-T-08-acceptance-checklist.md` | COMPLETE (توثيق) |
| Refresh/401/403 | interceptor + session tests | COMPLETE (آلي) |
| شبكة ضعيفة / إشعار بعد نقل / إذن معطّل | — | DEFERRED BY OWNER |
| Wi-Fi بإقرار جهاز + إنذار محلي | — | DEFERRED BY OWNER |
| Widget تكامل شاشات كاملة | شبه معدوم (widget_test دخول فقط) | PARTIALLY IMPLEMENTED |

---

## 5) مطابقة واجهات الموبايل لـ `design/index.html`

**مرجع التصميم:** `design/index.html` — قائمة `mobileMenu`: dashboard, devices, detail, events, eventDetail, commands, commandDetail, notifications, profile.

| شاشة التصميم | في التطبيق | Visual Fidelity | Functional |
|--------------|------------|-----------------|------------|
| login | `LoginPage` | PARTIAL (ثيم/RTL قريب؛ بدون بيانات تجريبية HTML) | COMPLETE (API حقيقي) |
| devices | `DevicesListPage` | PARTIAL (بطاقات/مؤشرات؛ بلا شريط تنقل سفلي كالتصميم) | COMPLETE |
| detail | `DeviceDetailPage` | PARTIAL (hero + أوامر؛ نصوص عربية ثابتة في أماكن) | COMPLETE (أوامر HTTPS) |
| events / commands | مدمجة في `DeviceHistoryPage` (تبويبان) | PARTIAL (قائمة بدل جداول التصميم) | COMPLETE |
| eventDetail / commandDetail | صفحات منفصلة | PARTIAL | COMPLETE |
| dashboard | **غير منفَّذ** | NOT IMPLEMENTED | NOT IMPLEMENTED |
| notifications (إذن) | **غير منفَّذ** كشاشة | NOT IMPLEMENTED | PARTIAL عبر NoOp port فقط |
| profile / إعدادات | **غير منفَّذ** (خروج من القائمة فقط إن وُجد) | NOT IMPLEMENTED | PARTIAL (logout في الجلسة) |
| Wi-Fi | موجود في التطبيق **وليس** في `index.html` | N/A (إضافة وظيفية SRS) | PARTIAL (هيكل) |

**ملاحظة مهمة:** بيانات `devices/events/commands` داخل HTML تجريبية؛ التطبيق **لا ينقلها** إلى المسارات الإنتاجية — صحيح.

**Visual Fidelity الإجمالي:** **PARTIAL / unverified pixel comparison** (لا لقطات شاشة جنب-إلى-جنب في هذا التدقيق).  
**Functional Acceptance (عميل):** **PARTIAL** بسبب الشاشات الناقصة + NoOp/Unavailable.

---

## 6) الربط بالـ Backend وعدم وجود بيانات وهمية إنتاجية

| المسار | Endpoint | مصدر بيانات UI | حكم |
|--------|----------|----------------|-----|
| دخول/تجديد/خروج | `/api/auth/*` (DefaultAuthPaths) | Backend | COMPLETE |
| أجهزة | `/api/owner/devices` | Backend | COMPLETE |
| أوامر | `/api/owner/devices/{id}/commands` | Backend | COMPLETE |
| حوادث | `/api/owner/devices/{id}/alarm-events` | Backend | COMPLETE |
| push-tokens | `/api/me/push-tokens` | Backend (عند وجود token) | PARTIAL — token من NoOp = لا تسجيل |
| إشعار بالفتح | `/api/owner/notifications/{id}` | Backend | COMPLETE منطقياً |
| Wi-Fi | محلي للجهاز | **Unavailable** | لا بيانات وهمية نجاح؛ أيضاً لا تكامل |

نماذج DTO (`OwnerDevice*`, `DeviceCommand*`, `AlarmEvent*`, `PushToken*`, `OwnerNotification*`) تطابق أسماء حقول Backend camelCase المراجعة (`OwnerDeviceDtos`, `OwnerCommandDtos`, `OwnerHistoryDtos`, `PushTokenDtos`).

**لا يوجد Mock/Fake في مسار الإنتاج**؛ الـ Fake مقتصر على الاختبارات. الاستثناءات الإنتاجية هي **NoOp** و**Unavailable** (سلوك صامت/صريح غير مكتمل وليس بيانات وهمية ناجحة).

---

## 7) TODO / Placeholder / Mock / Fake / NoOp / غير مكتمل

| العنصر | الموقع | الأثر |
|--------|--------|-------|
| `NoOpFcmMessaging` | `fcm_messaging_port.dart` + provider الافتراضي | لا token → لا register فعلي |
| `UnavailableLocalProvisioningClient` | `local_provisioning_client.dart` | لا soft-AP؛ نتيجة `unavailable` |
| `bootstrapPlaceholder` / `HomeShellPage` | `home_shell_page.dart` | شاشة يتيمة غير مربوطة بالموجّه (كود ميت) |
| `API_BASE_URL` placeholders staging/prod | `app_environment.dart` | متوقع للتطوير؛ ليس سراً |
| `notificationLinkLater` | تفاصيل الحادثة | ربط FCM UI مؤجّل |
| Fake* في `test/` فقط | عدة ملفات اختبار | مقبول |
| بطاقات `[ ]` داخل T-04/06/07/08 | `mobile-development-tasks.md` | فجوات موثّقة |

---

## 8) الجلسة / الصلاحيات / الذاكرة / أخطاء الشبكة

| الموضوع | التقييم |
|---------|---------|
| Access memory + Refresh secure | سليم |
| single-flight + تمييز 401/403 | سليم ومختبر |
| OWNER فقط على الدخول | سليم |
| auth guard | سليم |
| cache أجهزة/حوادث عند فقد صلاحية | سليم الاتجاه |
| revoke push قبل logout | منفَّذ؛ بلا أثر عملي مع NoOp |
| أخطاء شبكة في القوائم | empty/error + retry موجودة |
| تسجيل رموز | لم يُرصد تسريب في المسارات المفحوصة |

---

## 9) جودة الكود وإعادة الاستخدام

**إيجابيات**
- طبقات data/domain/presentation واضحة لأجهزة/أوامر/سجل.
- عدم retry على POST أوامر.
- فصل Wi-Fi عن عميل الخادم من حيث التصميم.

**ملاحظات جودة (ليست إصلاحات الآن)**
- تكرار ودجات `_Kv` / `_Notice` بين صفحات التفاصيل.
- نصوص عربية ثابتة في `device_detail_page` بجانب l10n.
- `HomeShellPage` ميت.
- لا حد أقصى واضح لمحاولات polling الأمر (إلغاء عند terminal فقط).
- تكرار جزئي لمنطق عرض الحالات بين card/detail.

---

## 10) توافق النماذج مع عقود Backend

| العقد | التوافق |
|-------|---------|
| Owner device list/detail + indicators | متوافق مع الحقول الأساسية |
| Commands request/response | متوافق (`type`, `idempotencyKey`, `targetAlarmEventUid`, الحالات) |
| Alarm events | متوافق (`wasBuffered`, `clockTrusted`, `occurredDeviceAt`, …) |
| Push tokens | متوافق |
| Notification detail | متوافق جزئياً (يُستخدم子集 للحقول اللازمة للتنقل) |
| OpenAPI مشترك في `integration` | **غير معتمد** لهذه المسارات — الاعتماد على DTOs Backend مباشرة |

---

## 11) الاختبارات — التغطية والفجوات

**الموجود (34):** session, interceptor, guard, JWT, devices entities/mappers/cache, commands entity, alarm mapper, open-notification, wifi unavailable, acceptance smoke, login widget.

**غير مغطى / ضعيف**
- Widget لتفاصيل الجهاز وإرسال أمر (PENDING UI).
- Widget للسجل/التصفية.
- تكامل Dio حي ضد Backend.
- polling TIMED_OUT → مصالحة → رفع الحظر عند `reconciledAt`.
- دورة FCM حقيقية / permission denied UI.
- soft-AP E2E.
- شبكة ضعيفة / هاتفان / نقل ملكية.
- لقطات مطابقة للتصميم.

---

## 12) مراجعة تعديلات الحوكمة (`AGENTS.md` / `task-execution.mdc` §6A)

| البند | التقييم |
|-------|---------|
| القاعدة الأصلية | إغلاق فقط بعد `APPROVE TASK CLOSURE: <TASK_ID>` |
| الاستثناء 6A | علّق البوابة لـ `MOB-T-*` في التسلسل الذاتي بتفويض مالك |
| التوافق مع القاعدة الأصلية | **متعمد ومقيّد بالموبايل**؛ الافتراضي ما زال سارياً لباقي المشاريع |
| المخاطر المكتشفة | 1) إغلاق مهمة `[x]` رغم بطاقات إلزامية مفتوحة (ACK/FCM-01) تحت مسمّى «نطاق عميل». 2) نسب تقدّم `100%` قد تُقرأ كقبول إنتاجي كامل. 3) الاستثناء ما زال نشطاً نصاً حتى إلغاء المالك |
| التوصية (بدون تنفيذ الآن) | الإبقاء على 6A كاستثناء مؤقت؛ عند الاستئناف العادي: إعادة بوابة الإغلاق، وتمييز صريح «client-closed» مقابل «production-closed» في التقدّم |

---

## 13) المشاكل البرمجية المكتشفة (للمراجعة — بلا إصلاح الآن)

1. **FCM الإنتاجي معطّل فعلياً** بسبب `NoOpFcmMessaging` رغم إغلاق T-06 جزئياً.
2. **Wi-Fi بلا قناة محلية** (`unavailable` دائماً).
3. **`HomeShellPage` كود ميت** + نص placeholder.
4. **شاشات تصميم ناقصة:** dashboard / notifications / profile (+ bottom nav).
5. **Polling أوامر بلا سقف زمني صريح** بعد TIMED_OUT (يعتمد حالة الأمر فقط).
6. **ترقيم سجل غير موجود** (بحث محلي فقط).
7. **نسبة 100% في `mobile-progress` مبالغ فيها** بالنسبة لقبول SRS الكامل.
8. **لا حزم Firebase** في `pubspec.yaml` — جاهزية FCM-01 صفر برمجياً.

---

## 14) الوظائف غير المكتملة · الاعتماديات · المؤجّل

### يمكن استكماله برمجياً الآن (بدون Firebase/ESP/G1/جهاز)

- شاشات dashboard / profile / هيكل إذن إشعارات وفق التصميم.
- l10n لنصوص التفاصيل الثابتة؛ إزالة/عزل `HomeShellPage`.
- Widget tests لمسارات الأوامر/السجل/الحراسة.
- حد أقصى/إلغاء أوضح لـ polling؛ اختبار مصالحة `reconciledAt`.
- تحسين ترقيم/نوافذ عرض القائمة محلياً.
- تمييز حالات التقدّم: client-closed vs production-closed (توثيق فقط إن طُلب).

### يعتمد على خارج الموبايل

| الاعتماد | يؤثر على |
|----------|----------|
| ملفات Firebase/APNs لكل flavor | T-06 FCM-01/03 الفعلي / FCM-06 |
| جهاز Android حقيقي | قبول FCM، إذن، fg/bg/terminated، T-08 ميداني |
| iOS + APNs | T-01/T-06 iOS |
| اعتماد G1 + ACK/STATE | T-04 بطاقات ACK، اتساق هاتفين، قبول إنتاجي أوامر |
| ESP-T-08 + إغلاق D-04 (عقد soft-AP) | T-07 إرسال محلي وتأكيد اتصال |
| Backend حي / Broker | تكامل E2E غير وحدوي |

---

## 15) نسبة إنجاز مقترحة (للتدقيق فقط — لم تُحدَّث ملفات التقدّم)

| مهمة | وزن تقريبي | إنجاز مقترح |
|------|-------------|-------------|
| T-01 | 10% | 95% |
| T-02 | 15% | 95% |
| T-03 | 15% | 88% |
| T-04 | 20% | 68% |
| T-05 | 12% | 85% |
| T-06 | 12% | 42% |
| T-07 | 8% | 35% |
| T-08 | 8% | 55% |
| **المجموع المرجّح** | 100% | **~72%** قبول وظيفي شامل · **~82%** إن احتُسب تأجيل المالك كما في 6A |

---

## 16) خطة استكمال الفجوات (مستقبلية — بانتظار موافقتك)

### المرحلة A — برمجي فقط
1. UI الناقص من التصميم (dashboard/profile/notifications shell).
2. تنظيف الكود الميت وl10n.
3. توسيع widget/unit tests للمسارات الحرجة.
4. ضبط polling/المصالحة في العميل.

### المرحلة B — بعد تسليم أسرار/ملفات Firebase
1. استبدال NoOp بتنفيذ Firebase Messaging حقيقي لكل flavor.
2. تفعيل register/onTokenRefresh/permission فعلياً.

### المرحلة C — بعد عقد ESP/D-04
1. تنفيذ `LocalProvisioningClient` HTTP محلي حقيقي.
2. حالات connected/failed/timeout من الجهاز.

### المرحلة D — بعد G1 + عتاد/ميداني
1. اختبارات ACK/STATE وترتيب الرسائل.
2. هاتفان، نقل ملكية، FCM حي، Wi-Fi مع استمرار إنذار محلي.
3. إعادة تقييم الإغلاق الإنتاجي تحت بوابة `APPROVE TASK CLOSURE` الافتراضية.

---

## 17) دروس هندسية مستفادة

1. **Interface/NoOp ≠ اكتمال:** وجود `FcmMessagingPort` و`LocalProvisioningClient` يثبت قابلية الحقن لا جاهزية المنتج.
2. **إغلاق نطاق عميل تحت 6A يحتاج تسمية صريحة** حتى لا تُخلط نسب التقدّم مع قبول SRS/G1.
3. **مصدر الحقيقة للأوامر يبقى ACK/حالة مؤكدة:** التطبيق يحترم ذلك في المسار الأساسي؛ ما تبقى هو إثبات الترتيب الحي بعد G1.
4. **التصميم المرجعي أشمل من الشاشات المنفَّذة:** الفجوة ليست فقط بصرية بل ملاحية (dashboard/profile/notifications).
5. **التحقق الآلي الحالي قوي للجلسة والملكية الدلالية، وضعيف للـ UI التفاعلي والتكامل الحي.**

---

## 18) خاتمة التدقيق

التطبيق جاهز كـ **عميل HTTPS للمالك** في المسارات الأساسية (جلسة، أجهزة، أوامر، سجل) مع أدلة analyze/test/APK حديثة.  
ليس جاهزاً كـ **قبول إنتاجي كامل** لمهام FCM الحي، Wi-Fi المحلي، ACK/G1، وشاشات التصميم المتبقية، والاختبارات الميدانية.

**بانتظار مراجعتك قبل أي تعديلات أو إعادة فتح مهام أو تصحيح نسب التقدّم.**
