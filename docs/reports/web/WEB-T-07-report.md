# تقرير المهمة WEB-T-07 — سجلات الإدارة والتنبيه الإداري

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | WEB-T-07 / T-07 |
| الاسم | سجلات الإدارة والتنبيه الإداري |
| الحالة | **CLOSED** — Autonomous Sequential Web Development (نطاق العميل) |
| التاريخ | 2026-11-18 |
| المشروع | `admin_web` فقط |
| الاستثناء | Autonomous Sequential Web — بلا انتظار `APPROVE TASK CLOSURE` |
| المرجع البصري | `design/index.html` شاشتا `audit` و`alerts` |

## 1. الملخص التنفيذي

أُضيفت شاشة سجل الإدارة على `/admin/audit-logs` مربوطة بـ `GET /api/admin/audit-logs` مع ترقيم وبحث وتصفية بمعاملات الخادم (`q`, `action`, `targetType`, `actorUserId`, `deviceId`, `targetId`, `result`, `from`, `to`, `page`, `pageSize`, `sortBy`, `sortDir`). معرفا الهدف والجهاز يبقيان ظاهرين لعمليات النقل والأرشفة.

إنشاء التنبيه يتم فقط عبر `POST /api/admin/alerts`. اللوحة تفصل أربعة أمور: قبول حفظ الطلب، تسجيل محاولة التسليم (`deliveryAttempted` مع `lastAttemptAt`)، نتيجة المزوّد (`ACCEPTED` فقط عند وجود المحاولة)، والقراءة. لا تُعرض «مقروء» إلا إذا أعاد الخادم `isRead=true` أو `readAt`. العقد الحالي لا يرسل دليل قراءة، فتبقى العبارة «لا دليل قراءة من الخادم». فشل التسليم `FAILED` يبقي العنوان والنص على الشاشة.

لا Firebase ولا FCM في الويب، ولا أسرار في المتصفح. مسارات الشاشات تحت `/admin` والحارس يرفض المالك. تسليم FCM الحي **NOT VERIFIED / BLOCKED**. تكامل المتصفح ضد خادم حي **NOT VERIFIED**. لم يُمس Backend ولا الموبايل ولا عقود integration.

## 2. البطاقات

- [x] شاشة سجلات الإدارة — نطاق العميل
- [x] إنشاء تنبيه وتتبع المحاولة عبر API فقط (`FR-026`) — نطاق العميل
- [x] ربط العرض بصلاحية الأدمن فقط — حارس المسار + 403 في الاختبارات
- تسليم FCM الحي — **NOT VERIFIED / BLOCKED** (ليس بطاقة منفصلة؛ عامل الإرسال خارج المتصفح)
- جلسة متصفح حية ضد Backend — **NOT VERIFIED**

## 3. الملفات (`admin_web`)

جديدة:

- `lib/features/admin/domain/entities/paged_page.dart`
- `lib/features/admin/domain/entities/audit_log_entity.dart`
- `lib/features/admin/domain/entities/admin_alert_entity.dart`
- `lib/features/admin/domain/admin_alert_outcome.dart`
- `lib/features/admin/domain/audit_log_query.dart`
- `lib/features/admin/domain/admin_alert_query.dart`
- `lib/features/admin/domain/create_admin_alert_input.dart`
- `lib/features/admin/domain/repositories/audit_logs_repository.dart`
- `lib/features/admin/domain/repositories/admin_alerts_repository.dart`
- `lib/features/admin/domain/usecases/audit_log_usecases.dart`
- `lib/features/admin/domain/usecases/admin_alert_usecases.dart`
- `lib/features/admin/data/mappers/audit_alert_mapper.dart`
- `lib/features/admin/data/datasources/audit_logs_remote_datasource.dart`
- `lib/features/admin/data/datasources/admin_alerts_remote_datasource.dart`
- `lib/features/admin/data/repositories/audit_logs_repository_impl.dart`
- `lib/features/admin/data/repositories/admin_alerts_repository_impl.dart`
- `lib/features/admin/presentation/admin_alert_facts.dart`
- `lib/features/admin/presentation/audit_labels.dart`
- `lib/features/admin/presentation/viewmodels/audit_logs_view_model.dart`
- `lib/features/admin/presentation/viewmodels/admin_alerts_view_model.dart`
- `lib/features/admin/presentation/pages/audit_logs_page.dart`
- `lib/features/admin/presentation/pages/admin_alerts_page.dart`
- `lib/features/admin/presentation/widgets/admin_alert_outcome_panel.dart`
- `test/features/admin/audit_alert_mapper_test.dart`
- `test/features/admin/admin_alert_flow_test.dart`

محدَّثة:

- `lib/core/network/app_endpoints.dart` — `adminAuditLogs` / `adminAlerts` / `adminAlert`
- `lib/core/router/app_routes.dart` و`app_router.dart` — `/admin/audit-logs` و`/admin/alerts`
- `lib/features/admin/presentation/providers/admin_providers.dart`
- `lib/features/admin/presentation/widgets/admin_shell.dart` — رابطا السجل والتنبيه
- `lib/features/admin/presentation/admin_copy.dart`
- `lib/features/admin/presentation/pages/admin_dashboard_page.dart`
- `lib/features/admin/presentation/widgets/admin_widgets.dart` — `AdminKvRow`
- `test/core/router/auth_guard_test.dart`

لم يُعدَّل `security_backEnd` ولا `security_mobile` ولا عقود `integration`.

## 4. السلوك

- GET للسجل والقائمة يستخدم `executeWithRetry`. POST الإنشاء لا يُعاد تلقائياً.
- `ACCEPTED` أو `FAILED` بلا `deliveryAttempted` و`lastAttemptAt` لا يُحتسب نتيجة تسليم.
- 403 يفرّغ صفوف السجل ولا يعلّم إنشاء التنبيه ناجحاً ولا يبقي تفاصيل تنبيه بعد رفض التفصيل.
- لا مسار جلب لسجل التدقيق بالمعرف؛ التفاصيل من صف الصفحة المحمّلة. التنبيه له `GET /api/admin/alerts/{id}`.
- النموذج الأولي في `design/index.html` لشاشة `alerts` يعرض أعطال أجهزة تجريبية. ذلك يخالف `FR-026`، لذلك تعرض الشاشة عنوان التنبيه ونصه ونتيجة محاولة الخادم.

## 5. الاختبارات والبناء

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` في `admin_web` | exit 0 — No issues found |
| `flutter test` في `admin_web` | exit 0 — **76/76** passed |
| `flutter build web --release` في `admin_web` | exit 0 — `Built build\web` (حوالي 381 ثانية) |

اختبارات هذه المهمة: `audit_alert_mapper_test.dart` (5) و`admin_alert_flow_test.dart` (5) وتمديد `auth_guard_test.dart`. لم يُشغَّل متصفح ضد API حي، ولم يُتحقق تسليم FCM على جهاز.

## 6. المصفوفة

| البند | الحكم |
|--------|--------|
| شاشة السجل والترقيم/الاستعلام | VERIFIED (عميل) |
| معرفات النقل/الأرشفة | VERIFIED (تعيين JSON) |
| إنشاء تنبيه وعرض نتيجة المحاولة | VERIFIED (عميل) |
| عدم ادّعاء قراءة بلا دليل | VERIFIED |
| بقاء التنبيه عند FAILED | VERIFIED (لوحة النتيجة) |
| حصر المسار بالأدمن | VERIFIED (حارس) / جلسة حية NOT VERIFIED |
| 403 لا يُعرض كنجاح | VERIFIED (وحدة) |
| تسليم FCM الحي | NOT VERIFIED / BLOCKED |
| مطابقة بصرية مع `index.html` | PARTIAL — لم تُلتقط لقطات مقارنة |

## 7. الأثر

لا تغيير على المصادقة أو إبطال الجلسة أو عزل الملكية أو المخطط أو MQTT أو بوابات G0/G1/G2. الويب لا ينشئ عميل FCM.

## 8. الإغلاق

نطاق العميل **CLOSED**. التالية `WEB-T-08`. المؤشرات بعد المزامنة: الويب **7/8**، والمجموع **20/50**. بطاقة T-06 كانت معلَّمة مغلقة في `web-development-tasks.md` قبل هذه الجولة؛ عُدّلت مؤشرات التقدم لتتوافق مع البطاقات عند إغلاق T-07.

## الدرس الهندسي المستفاد

أُضيف هذا القسم في 2026-11-18 أثناء `WEB-C-06` لاستكمال التوثيق. لا يغيّر لقطة **76/76** ولا مدة البناء المسجّلة أعلاه.

حفظ التنبيه غير تسليمه. `AdminAlertOutcome.requestAccepted` يكفي له معرف غير فارغ، بينما `deliverySucceeded` يشترط `deliveryAttempted` و`lastAttemptAt` وحالة `ACCEPTED`. `ACCEPTED` أو `FAILED` بلا طابع المحاولة لا يُحسب تسليمًا، والقراءة لا تُستنتج. النص الظاهر في `admin_alert_facts.dart` يقول إن قبول الحفظ ليس نجاح تسليم. يُستخدم الفصل عندما يعيد الخادم سجل محاولة منفصلًا عن إنشاء المورد. التحقق عند إغلاق T-07: `create result shows server attempt facts and no read claim` في `admin_alert_flow_test.dart`. تسليم FCM الحي بقي **NOT VERIFIED / BLOCKED**.
