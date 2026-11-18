# قائمة تحقق قبول الويب مقابل SRS

| الحقل | القيمة |
|--------|--------|
| المهمة | WEB-T-08 |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` — Software-Only |
| مجموعة الاختبار الختامية | `flutter test` في `admin_web` — **80/80** |
| مرجع SRS | `docs/SRS_نظام_الأمان_الذكي_v1.0.md` القسم 10.1 و`WEB-07` في خطة التطوير |

الأحكام في الجداول أدناه لعميل Flutter Web عند إغلاق `WEB-T-08`، ولقطة المجموعة حينها **80/80**. نجاح الاختبار لا يُغلق G0 أو G1 أو G2، ولا يثبت جهازاً أو متصفحاً ضد Backend حي.

## تحديث مرحلة الاستكمال — 2026-11-18

لا يلغي صفوف `WEB-T-08` تحت هذا العنوان.

- إعادة تشغيل `WEB-C-06`: `flutter test` في `admin_web` **121/121**. الاختبارات الحرجة القديمة ما زالت داخل المجموعة (ذاكرة Access، single-flight، ترويسة CSRF، دلالات الأمر، غياب أوامر المالك).
- صف «مطابقة بصرية مع `design/index.html`» كان `NOT VERIFIED` عند T-08. بعد C-03…C-06 صار **PARTIAL**: لقطات معاينة ومقارنة دخول النسخة المبنية، بلا تطابق بكسل وبلا لقطة متصفح لكل الشاشات. انظر `WEB-EXT-08`.
- `WEB-EXT-09` اكتمل 2026-11-18 بتحميل `build/web` محليًا. ذلك لا يغيّر أحكام الحي في الجداول.
- لا صف حي انتقل إلى PASSED.

## القسم 10.1 — واجهات المستخدم

| البند | الحكم | الدليل |
|--------|--------|--------|
| تسجيل الدخول يعرض الشاشة ثم يوجّه حسب الجلسة | PASSED (عميل) | `admin_web/test/widget_test.dart`؛ `test/core/router/auth_guard_test.dart`؛ `test/features/acceptance/web_acceptance_smoke_test.dart` |
| قائمة أجهزة المالك | PASSED (عميل) | `test/features/devices/owner_device_entity_test.dart`؛ `test/features/devices/devices_repository_cache_test.dart` |
| تفاصيل الجهاز تميّز المعلّق/المرفوض/المهلة عن التنفيذ المؤكد | PASSED (دلالات عميل) | `web_acceptance_smoke_test.dart`؛ `device_command_entity_test.dart`؛ `device_detail_command_guards_test.dart`؛ `command_poll_policy_test.dart` |
| ACK الحي يحدّث الحالة المؤكدة من الجهاز | BLOCKED | لا جهاز ولا اعتماد G1 في هذا التسلسل. `WEB-EXT-04` |
| الأحداث والسجل | PASSED (عميل) | `device_history_view_model_test.dart`؛ `history_query_test.dart`؛ `alarm_event_mapper_test.dart`؛ `alarm_event_detail_page_test.dart` |
| لوحة الأدمن: حسابات، أجهزة، سجل، تنبيه | PASSED (عميل) | `auth_guard_test.dart`؛ `web_acceptance_smoke_test.dart`؛ `admin_device_detail_page_test.dart`؛ `admin_alert_flow_test.dart`؛ `audit_alert_mapper_test.dart` |
| مطابقة بصرية مع `design/index.html` | NOT VERIFIED عند WEB-T-08 | لم تُلتقط لقطات مقارنة في تسلسل T. تحديث 2026-11-18: PARTIAL بعد لقطات الاستكمال ومقارنة دخول `build/web`، بلا تطابق بكسل (`WEB-EXT-08`) |

## متطلبات وظيفية يلمسها الويب

| المعرف | الحكم | الدليل |
|--------|--------|--------|
| FR-001 الدخول | PASSED (عميل) | `auth_session_controller_test.dart`؛ `widget_test.dart` |
| FR-002…FR-008 إدارة الحساب والجهاز والربط والنقل من الواجهة | PASSED (عميل) / الحي NOT VERIFIED | `owner_account_write_test.dart`؛ `admin_ownership_rules_test.dart`؛ `admin_device_detail_page_test.dart` |
| FR-010…FR-013 أوامر الحماية والكتم والاختبار | PASSED (حواجز عميل) | `device_detail_command_guards_test.dart` |
| FR-014 دورة حالة الأمر | PASSED (عميل) | `web_acceptance_smoke_test.dart` — `PENDING` ليس تنفيذاً، `EXECUTED` وحده تنفيذ مؤكد، `REJECTED` نهائي بلا تنفيذ مؤكد، `TIMED_OUT` غير محسوم حتى `reconciledAt` |
| FR-015 تأكيد متأخر وحالة فعلية | PARTIAL | العميل لا يقلب المؤشر عند HTTP `PENDING`. ACK الحي BLOCKED |
| FR-016 أوامر متعارضة | PASSED (عميل) | `device_detail_command_guards_test.dart`؛ `command_poll_policy_test.dart` |
| FR-017 منتهٍ/مكرر على خادم حي | NOT VERIFIED | لا مسار G1 حي |
| FR-018 جهاز غير متصل | PASSED (عميل) | `owner_device_entity_test.dart` |
| FR-023 وقت الحدث بلا مدة مختلقة | PASSED (عميل) | `alarm_event_mapper_test.dart` |
| FR-026 تنبيه إداري | PASSED (عميل) / تسليم FCM BLOCKED | `admin_alert_flow_test.dart`؛ `audit_alert_mapper_test.dart` |
| FR-032 سجل المالك | PASSED (عميل) | `history_query_test.dart`؛ `device_history_view_model_test.dart` |
| FR-033 سجل الإدارة | PASSED (عميل) | `audit_alert_mapper_test.dart`؛ `admin_alert_flow_test.dart` |
| FR-035 ربط PENDING | PASSED (عميل) | `owner_device_entity_test.dart`؛ `admin_ownership_rules_test.dart`؛ `admin_device_detail_page_test.dart` |
| FR-036 إعداد Wi-Fi | خارج الويب | `security_mobile` + `security_esp` |
| WEB-07 أمر ناجح/مرفوض/مهلة | PASSED (دلالات عميل) | `web_acceptance_smoke_test.dart` |
| WEB-07 نقل ملكية حي | NOT VERIFIED | `WEB-EXT-05` |

## الجلسة والصلاحية

| البند | الحكم | الدليل |
|--------|--------|--------|
| Access في الذاكرة فقط | PASSED | `auth_session_controller_test.dart` (`login stores access in memory only`)؛ `web_acceptance_smoke_test.dart` |
| Refresh single-flight عند 401 المتزامن | PASSED | `auth_interceptor_test.dart` |
| 403 لا يجدّد الجلسة ولا يُعرض كنجاح كتابة | PASSED | `auth_interceptor_test.dart`؛ `owner_account_write_test.dart`؛ `admin_alert_flow_test.dart` |
| إرفاق ترويسة `X-CSRF-TOKEN` | PASSED (عميل) | `csrf_header_interceptor_test.dart`؛ اسم الترويسة في `web_acceptance_smoke_test.dart` |
| CSRF خاطئ / Origin أجنبي / CORS+Credentials ضد Backend حي | BLOCKED / NOT VERIFIED | `WEB-EXT-01` |
| استعادة الجلسة بعد Reload في متصفح حقيقي | NOT VERIFIED | `WEB-EXT-02`. غياب CSRF في الاختبار يبقي الجلسة غير مصادَق عليها: `restore without csrf cookie stays unauthenticated` |
| خروج يمسح Access ويمنع إحياء الجلسة ويفرغ السجل المعروض | PASSED (عميل) | `auth_session_controller_test.dart`؛ `device_history_view_model_test.dart`؛ `devices_repository_cache_test.dart`؛ `alarm_events_repository_cache_test.dart`؛ `web_acceptance_smoke_test.dart` |
| تعطيل حساب حي يُبطل الجلسة فوراً | BLOCKED / NOT VERIFIED | `WEB-EXT-03` |
| فصل ADMIN / OWNER | PASSED (حارس عميل) | `auth_guard_test.dart`؛ `web_acceptance_smoke_test.dart` |
| لا أزرار أوامر مالك على شاشة جهاز الأدمن | PASSED | `admin_device_detail_page_test.dart` |
| نقل ملكية حي يُفرّغ سجل المالك السابق | NOT VERIFIED | تبديل المستخدم في العميل PASSED (`device_history_view_model_test.dart`). الخادم المتصل: `WEB-EXT-05` |
| G1 / COMMAND_ACK الحي | BLOCKED | `WEB-EXT-04` |
| تسليم FCM الحي للتنبيه الإداري | BLOCKED / NOT VERIFIED | `WEB-EXT-07` |
| متصفح تفاعلي ضد Backend حقيقي | NOT VERIFIED | `WEB-EXT-06` |
| NFR-002 أمن النقل end-to-end | PARTIAL | العميل يستخدم HTTPS عبر Dio ولا يخزن Access في Web Storage. رفض CSRF/Origin الحي NOT VERIFIED |
| NFR-003 عزل الملكية على الخادم | PARTIAL | الحارس و403 وإفراغ الذاكرة PASSED. النقل الحي NOT VERIFIED |
| G0 / G1 / G2 | NOT VERIFIED | لم تُغلق في هذا التسلسل ولم يُخترع اعتماد لها |

## ما لا يُحتسب نجاحاً

- HTTP 202 أو حالة `PENDING` ليست تنفيذ جهاز.
- `EXECUTED` في الاختبار هو دلالة كيان بعد حالة أعادها مصدر مُحاكى، وليس ACK من ESP32.
- `TIMED_OUT` يبقى غير مؤكد حتى `reconciledAt`.
- حارس المسار ليس بديلاً عن 403 من API.
