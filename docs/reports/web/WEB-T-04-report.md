# تقرير المهمة WEB-T-04 — تفاصيل المالك والأوامر

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | WEB-T-04 / T-04 |
| الاسم | تفاصيل المالك والأوامر |
| الحالة | **CLOSED** — Autonomous Sequential Web Development (نطاق العميل) |
| التاريخ | 2026-11-17 |
| المشروع | `admin_web` فقط |
| الاستثناء | Autonomous Sequential Web — بلا انتظار `APPROVE TASK CLOSURE` |

## 1. الملخص التنفيذي

أُضيفت شاشة تفاصيل جهاز المالك على `/owner/devices/:deviceId` مع أوامر `ARM` / `DISARM` / `MUTE_ALARM` / `TEST_SIREN` / `STOP_TEST`. قبول HTTP بحالة `PENDING` لا يقلب المؤشرات المؤكدة. الاستعلام يتم بـ GET فقط، وPOST لا يُعاد تلقائياً. مسارات الأدمن لا تعرض شاشة الأوامر. اعتماد G1 وACK الحي وسيناريو OWNERSHIP_SYNC على جهاز متصل **BLOCKED**.

## 2. البطاقات

- [x] WEB-04 شاشة التفاصيل وتدفق الأمر — نطاق العميل
- [x] قيود التعارض والمهلة في الواجهة
- [x] MUTE/TEST وفق SRS
- [ ] اختبار نقل ملكية جهاز متصل حتى إقرار OWNERSHIP_SYNC — **BLOCKED**

## 3. الملفات (admin_web)

جديدة:

- `lib/features/devices/domain/entities/device_command_entity.dart`
- `lib/features/devices/domain/command_poll_policy.dart`
- `lib/features/devices/domain/repositories/device_commands_repository.dart`
- `lib/features/devices/data/models/device_command_dto.dart`
- `lib/features/devices/data/mappers/device_command_mapper.dart`
- `lib/features/devices/data/datasources/device_commands_remote_datasource.dart`
- `lib/features/devices/data/repositories/device_commands_repository_impl.dart`
- `lib/features/devices/presentation/viewmodels/device_detail_view_model.dart`
- `lib/features/devices/presentation/pages/device_detail_page.dart`
- `lib/features/devices/presentation/widgets/info_notice.dart`
- `lib/core/router/app_route_observer.dart`
- `test/features/devices/device_command_entity_test.dart`
- `test/features/devices/command_poll_policy_test.dart`
- `test/features/devices/device_commands_repository_test.dart`
- `test/features/devices/device_detail_command_guards_test.dart`

محدَّثة:

- `lib/core/network/app_endpoints.dart` — `ownerDeviceCommands` / `ownerDeviceCommand`
- `lib/core/router/app_routes.dart` — `/owner/devices/:deviceId`
- `lib/core/router/app_router.dart`
- `lib/features/devices/presentation/providers/devices_providers.dart` — `deviceCommandsRepositoryProvider` عبر `webDioProvider`
- `lib/features/devices/presentation/pages/devices_list_page.dart`
- `lib/features/devices/presentation/widgets/device_card.dart`
- `test/core/router/auth_guard_test.dart`

لم يُعدَّل `security_mobile` ولا `security_backEnd` ولا عقود `integration`.

## 4. السلوك

- ACTIVE يفتح التفاصيل؛ PENDING غير قابل للضغط.
- الأدمن على `/owner/devices/:id` يُحوَّل إلى `/admin`. لا مسار أمر تحت `/admin`.
- MUTE يتطلب `currentAlarmEventUid`. TEST يُمنع أثناء `ALARM`. STOP_TEST فقط أثناء `TEST`.
- `TIMED_OUT` بدون `reconciledAt` يمنع أمراً متعارضاً. إيقاف الاستطلاع لا يخترع `EXECUTED`.
- إعادة التحميل عند فتح الصفحة، وعند `AppLifecycleState.resumed`، وعند `RouteAware.didPopNext`.
- زر محاكاة ACK في `design/index.html` لم يُنقل (سلوك تجريبي). رابط السجل مؤجّل إلى WEB-T-05. إعداد Wi-Fi خارج الويب.

## 5. الاختبارات والبناء

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` (في `admin_web`) | No issues found |
| `flutter test` | **38/38** |
| `flutter build web --release` | `√ Built build\web` |
| ACK حي / G1 / OWNERSHIP_SYNC على جهاز | **BLOCKED / NOT VERIFIED** |
| جلسة متصفح ضد Backend حي | **NOT VERIFIED** |

## 6. القبول

| الشرط | النتيجة |
|--------|---------|
| نجاح HTTP لا يغيّر المؤشر المؤكد | VERIFIED — `device_detail_command_guards_test` |
| TIMED_OUT لا يدّعي فشل التنفيذ ويبقي حظر التعارض | VERIFIED — كيان + سياسة استطلاع + حارس ViewModel |
| لا أوامر أدمن عبر مسارات الأدمن | VERIFIED — الحارس يعيد الأدمن من مسار المالك؛ لا route تحت `/admin` |
| MUTE بلا معرف حادثة يُمنع | VERIFIED — لا POST |
| TEST أثناء ALARM / STOP_TEST خارج TEST | VERIFIED |
| لا إعادة تلقائية لـ POST | VERIFIED — `device_commands_repository_test` |
| ACK يحدّث المؤشر من الحالة الفعلية على خادم حي | BLOCKED |
| نقل ملكية متصل حتى OWNERSHIP_SYNC | BLOCKED |

### Visual Fidelity

مطابقة جزئية وغير مُتحقق منها بلقطة متصفح. المصدر البصري: `design/index.html` (شاشة تفاصيل المالك) مع نقل سلوك `security_mobile` `DeviceDetailPage`. المؤشرات الأربع تُعرض كحبوب حالة كما في WEB-T-03، لا كبطاقات مقياس في النموذج. لم تُدعَ مطابقة بكسل.

### Functional Acceptance

نطاق العميل **VERIFIED** بالاختبارات أعلاه. المسار الحي **BLOCKED**.

## 7. النسبة والمهمة التالية

ويب **4/8 (50%)**. المجموع النظامي **17/50 (34%)**. التالية: **WEB-T-05**.

## الدرس الهندسي المستفاد

`CommandStatus.pending` وHTTP 202 ليسا تنفيذاً. المؤشر المؤكد يتغير فقط بعد إعادة تحميل الجهاز عندما يعيد GET حالة نهائية، بينما `TIMED_OUT` يبقى حاجزاً للتعارض حتى `reconciledAt`. المرجع: `DeviceDetailViewModel.submitCommand` و`CommandPollPolicy.pollStopClearsConflictBlock`.
