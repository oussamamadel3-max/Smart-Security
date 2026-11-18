# تقرير المهمة WEB-T-05 — سجل أحداث وأوامر المالك

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | WEB-T-05 / T-05 |
| الاسم | سجل أحداث وأوامر المالك |
| الحالة | **CLOSED** — Autonomous Sequential Web Development (نطاق العميل) |
| التاريخ | 2026-11-17 |
| المشروع | `admin_web` فقط |
| الاستثناء | Autonomous Sequential Web — بلا انتظار `APPROVE TASK CLOSURE` |
| المرجع البصري | `design/index.html` شاشات `events` / `eventDetail` / `commands` / `commandDetail` |

## 1. الملخص التنفيذي

أُضيف سجل ملكية المالك على `/owner/devices/:deviceId/history` مع تفاصيل الحادثة والأمر. واجهة API الفعلية (`OwnerDeviceAlarmEventsController` و`OwnerDeviceCommandsController`) تعيد قائمة فترة الملكية كاملة بلا معاملات صفحة أو بحث، لذلك البحث وترقيم الصفحات (8 صفوف) يتمان على هذه القائمة محلياً. `clockTrusted=false` يُسقط وقت الوقوع ويعرض «غير مؤكد» دون حساب مدة تأخير. معرف أجنبي أو 403/404 يُفرّغ الذاكرة المؤقتة ويعرض الخطأ. تسجيل الخروج وتبديل الحساب يُفرغان السجل المعروض.

نقل الملكية على خادم متصل **NOT VERIFIED**. لم يُمس G1 ولا `security_mobile` ولا Backend.

## 2. البطاقات

- [x] WEB-05 قائمة السجل والتفاصيل — نطاق العميل
- [x] عرض التأخر/wasBuffered دون مدة مختلقة
- [x] حراسة مسار السجل بالملكية — حارس المسار + 403/404 + إفراغ الجلسة
- نقل ملكية حي على خادم متصل — **NOT VERIFIED** (ليس بطاقة منفصلة؛ العزل الحي للخادم لم يُشغَّل هنا)

## 3. الملفات (`admin_web`)

جديدة:

- `lib/features/devices/domain/entities/alarm_event_entity.dart`
- `lib/features/devices/domain/history_query.dart`
- `lib/features/devices/domain/history_session_policy.dart`
- `lib/features/devices/domain/repositories/device_alarm_events_repository.dart`
- `lib/features/devices/domain/usecases/list_device_alarm_events_usecase.dart`
- `lib/features/devices/domain/usecases/get_device_alarm_event_usecase.dart`
- `lib/features/devices/domain/usecases/list_device_commands_usecase.dart`
- `lib/features/devices/domain/usecases/get_device_command_usecase.dart`
- `lib/features/devices/data/models/alarm_event_dto.dart`
- `lib/features/devices/data/mappers/alarm_event_mapper.dart`
- `lib/features/devices/data/datasources/device_alarm_events_remote_datasource.dart`
- `lib/features/devices/data/repositories/device_alarm_events_repository_impl.dart`
- `lib/features/devices/presentation/history_format.dart`
- `lib/features/devices/presentation/viewmodels/device_history_view_models.dart`
- `lib/features/devices/presentation/pages/device_history_page.dart`
- `lib/features/devices/presentation/pages/alarm_event_detail_page.dart`
- `lib/features/devices/presentation/pages/command_detail_page.dart`
- `lib/features/devices/presentation/widgets/history_kv_row.dart`
- `test/features/devices/alarm_event_mapper_test.dart`
- `test/features/devices/history_query_test.dart`
- `test/features/devices/alarm_events_repository_cache_test.dart`
- `test/features/devices/device_history_view_model_test.dart`
- `test/features/devices/alarm_event_detail_page_test.dart`

محدَّثة:

- `lib/core/network/app_endpoints.dart` — `ownerDeviceAlarmEvents` / `ownerDeviceAlarmEvent`
- `lib/core/router/app_routes.dart` و`app_router.dart`
- `lib/features/devices/presentation/providers/devices_providers.dart` — المستودعات عبر `webDioProvider` مع إفراغ عند فقد الجلسة
- `lib/features/devices/data/repositories/device_commands_repository_impl.dart` — ذاكرة سجل تُفرَّغ عند 401/403/404
- `lib/features/devices/presentation/pages/device_detail_page.dart` — زر السجل خارج عرض `PENDING`
- `test/core/router/auth_guard_test.dart`
- `test/features/devices/device_detail_command_guards_test.dart` — توقيع المستودع

لم يُعدَّل `security_mobile` ولا `security_backEnd` ولا عقود `integration`.

## 4. السلوك

- تبويبا الحوادث والأوامر. أمر `TEST_SIREN` / `STOP_TEST` يُوسم «اختبار صفارة». الحادثة تبقى «حادثة إنذار».
- `wasBuffered` يُعرض كعلامة ونص `bufferedNotice`. لا حقل مدة تأخير.
- `clockTrusted=false` يُهمل `occurredDeviceAt` حتى لو وصل في JSON.
- `TIMED_OUT` في تفاصيل الأمر يبقى نتيجة غير محسومة.
- الأدمن على مسار `/owner/.../history` يُحوَّل إلى `/admin`.
- 403/404 أو معرف لا يطابق الاستجابة يُفرّغ ذاكرة الجهاز ويعرض الخطأ بلا صف.
- الخروج وتبديل `user.id` يُفرغان المستودع والحالة المعروضة.
- لا mock إنتاجي. اختبارات الوحدة تستخدم مزيفات داخل `test/` فقط.

## 5. الاختبارات والبناء

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` (في `admin_web`) | No issues found |
| `flutter test` | **58/58** |
| `flutter build web --release` | `√ Built build\web` |
| نقل ملكية حي / Backend متصل | **NOT VERIFIED** |
| جلسة متصفح ضد Backend حي | **NOT VERIFIED** |
| خدمة الملفات الثابتة لـ `build/web` | **NOT VERIFIED** — `python` و`py` غير متاحين في هذه الجلسة |

## 6. القبول

| الشرط | النتيجة |
|--------|---------|
| قائمة مع بحث وترقيم وفق API الفعلي | VERIFIED — `history_query_test`؛ GET بلا query |
| تمييز حادثة / أمر / اختبار صفارة | VERIFIED — `isSirenTestCommand` + عناوين الواجهة |
| `wasBuffered` بلا مدة مختلقة | VERIFIED — `alarm_event_mapper_test` |
| `clockTrusted=false` عبارة وقوع غير مؤكد | VERIFIED — mapper يسقط الوقت؛ النص `occurrenceUntrusted` |
| تفاصيل حادثة | VERIFIED — مسار وViewModel |
| معرف أجنبي لا يعرض بيانات | VERIFIED — `alarm_event_detail_page_test` و`device_history_view_model_test` |
| 403/404 يُفرّغ الذاكرة | VERIFIED — `alarm_events_repository_cache_test` |
| إفراغ عند الخروج وتبديل الحساب | VERIFIED — `device_history_view_model_test` |
| حارس مسار المالك | VERIFIED — `auth_guard_test` |
| زر السجل من التفاصيل | VERIFIED بالمسار في `DeviceDetailPage` خارج `PENDING` |
| نقل ملكية حي على الخادم | NOT VERIFIED |
| مطابقة بكسل مع `design/index.html` | NOT VERIFIED |

### Visual Fidelity

مطابقة جزئية وغير مُتحقق منها بلقطة متصفح للشاشة الممتلئة. المصدر: `design/index.html`. نُقلت لغة الألوان عبر `DesignTokens` (بطاقة بيضاء، حد، ظل، بحث، جدول عريض وبطاقات تحت 900px، RTL عبر `Locale('ar')`). اختبار الودجت أكد نص الخطأ لمعرف أجنبي فقط. لم تُدعَ مطابقة بكسل.

### Functional Acceptance

نطاق العميل **VERIFIED** بالاختبارات أعلاه. المسار الحي ونقل الملكية على خادم متصل **NOT VERIFIED**.

## 7. النسبة والمهمة التالية

ويب **5/8 (62.5%)**. المجموع النظامي **18/50 (36%)**. التالية: **WEB-T-06**.

## الدرس الهندسي المستفاد

قائمة السجل عقدها GET بلا `page` ولا `q`. ترقيم الواجهة نافذة على الحمولة نفسها (`sliceHistoryPage` بحجم 8)، و`AlarmEventMapper.toEntity` يُسقط `occurredDeviceAt` عندما `clockTrusted=false` حتى لا تُختلق مدة تأخير من وقت الجهاز. عملياً: أي زمن ظهور جديد يجب أن يأتي من حقل الخادم الموثوق، لا من فرق بين `receivedAt` ووقت غير موثوق.
