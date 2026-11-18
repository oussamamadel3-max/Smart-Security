# تقرير المهمة WEB-T-08 — اختبارات القبول للويب

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | WEB-T-08 / T-08 |
| الاسم | اختبارات القبول للويب |
| الحالة | **CLOSED** — Autonomous Sequential Web Development (نطاق مجموعة قبول العميل) |
| التاريخ | 2026-11-18 |
| المشروع | `admin_web` فقط |
| الاستثناء | Autonomous Sequential Web — بلا انتظار `APPROVE TASK CLOSURE` |
| التالية | لا مهمة `WEB-T-*` داخل التسلسل |

## 1. الملخص التنفيذي

أُضيف ملف دخان قبول يجمّع ما يمكن إثباته برمجياً: مسار المالك من بوابة الدخول إلى مسارات الجهاز والسجل والأمر، ومسار الأدمن على الحسابات والأجهزة والسجل والتنبيه مع منع أوامر المالك، ودلالات `PENDING` / `EXECUTED` / `REJECTED` / `TIMED_OUT`، ومسح Access في الذاكرة مع سياسة إفراغ سجل التاريخ عند فقد المصادقة.

اختبارات الجلسة والصلاحية التي كانت موجودة بقيت هي الدليل التفصيلي: Access في الذاكرة، single-flight، إرفاق `X-CSRF-TOKEN`، و403 لا يجدّد الجلسة. قائمة SRS كُتبت في `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`.

لم يُنفَّذ CSRF/Origin/CORS ضد خادم حي، ولا تعطيل حساب حي، ولا G1/ACK، ولا نقل ملكية حي، ولا FCM حي، ولا متصفح تفاعلي ضد Backend حقيقي. هذه البنود **BLOCKED / NOT VERIFIED**. المحاكاة ليست إثبات إنتاج. لم يُمس Backend ولا الموبايل ولا ESP ولا عقود integration، ولم يُغلق G0 أو G1 أو G2.

## 2. البطاقات

- [x] WEB-07 دلالات أمر ناجح/مرفوض/منتهي المهلة — نطاق العميل
- [x] جلسة يمكن التحقق منها برمجياً: ذاكرة Access، single-flight، ترويسة CSRF، 401/403، إفراغ الذاكرة/السجل
- [x] قائمة تحقق SRS — `WEB-ACCEPTANCE-CHECKLIST.md`
- CSRF/Origin/CORS الحي، تعطيل الحساب الحي، ACK/G1 الحي، نقل الملكية الحي، FCM الحي، المتصفح التفاعلي — **BLOCKED / NOT VERIFIED** (بطاقة حية تبقى غير معلَّمة في `web-development-tasks.md`)

## 3. الملفات

جديد:

- `admin_web/test/features/acceptance/web_acceptance_smoke_test.dart`
- `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`
- `docs/reports/web/WEB-T-08-report.md`
- `docs/reports/web/WEB-FINAL-REPORT.md`

محدَّث:

- `admin_web/web-development-tasks.md`
- `admin_web/web-current.md`
- `admin_web/web-progress.md`
- `admin_web/web-context.md`
- `admin_web/WEB-REMAINING-EXTERNAL-WORK.md`
- `system-progress.md`
- `system-context.md`

لم يُعدَّل `security_backEnd` ولا `security_mobile` ولا `security_esp` ولا عقود `integration`.

## 4. ما يغطيه الدخان وما يبقى في اختبارات سابقة

| الموضوع | الحكم | الملف |
|---------|--------|--------|
| مسار المالك: غير مصادق → دخول؛ المالك يبقى على التفاصيل والسجل والأمر؛ انتهاء الجلسة يعيد للدخول | VERIFIED | `web_acceptance_smoke_test.dart` |
| مسار الأدمن: حسابات/أجهزة/سجل/تنبيه؛ المالك يُرفض؛ الأدمن لا يفتح مسار أمر المالك | VERIFIED | `web_acceptance_smoke_test.dart` و`auth_guard_test.dart` |
| لا أزرار أوامر مالك على شاشة جهاز الأدمن | VERIFIED | `admin_device_detail_page_test.dart` |
| `PENDING` ليس تنفيذاً؛ `EXECUTED` تنفيذ مؤكد؛ `REJECTED` نهائي بلا تنفيذ مؤكد؛ `TIMED_OUT` يمنع التعارض حتى `reconciledAt` | VERIFIED (دلالات) | `web_acceptance_smoke_test.dart`؛ `device_detail_command_guards_test.dart` |
| Access في الذاكرة ورفض أدوار خارج OWNER/ADMIN | VERIFIED | `auth_session_controller_test.dart` |
| single-flight لعدة 401 | VERIFIED | `auth_interceptor_test.dart` |
| إرفاق CSRF عند العلم، وعدم إرفاقه بدونه | VERIFIED | `csrf_header_interceptor_test.dart` |
| الخروج يمسح الرمز ويمنع إحياء الجلسة ويفرغ السجل المعروض | VERIFIED | `auth_session_controller_test.dart`؛ `device_history_view_model_test.dart`؛ `web_acceptance_smoke_test.dart` |
| 403 لا يُعامل كخروج ولا كنجاح كتابة | VERIFIED | `auth_interceptor_test.dart`؛ `owner_account_write_test.dart`؛ `admin_alert_flow_test.dart` |
| ACK حي | BLOCKED | `WEB-EXT-04` |
| CSRF/Origin/CORS حي وتعطيل حساب حي | BLOCKED / NOT VERIFIED | `WEB-EXT-01` `WEB-EXT-03` |
| نقل ملكية حي | NOT VERIFIED | `WEB-EXT-05` |
| FCM حي | BLOCKED / NOT VERIFIED | `WEB-EXT-07` |
| متصفح ضد Backend حقيقي | NOT VERIFIED | `WEB-EXT-06` |

## 5. الاختبارات والبناء

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` في `admin_web` | exit 0 — No issues found (21.5s) |
| `flutter test` في `admin_web` | exit 0 — **80/80** passed (كان 76 قبل ملف الدخان) |
| `flutter build web --release` في `admin_web` | exit 0 — `Built build\web` (حوالي 395 ثانية؛ تجميع `lib\main.dart` 370.4s) |

اختبارات هذه المهمة الأربعة داخل `web_acceptance_smoke_test.dart`. لم يُشغَّل متصفح ضد API حي.

## 6. الأثر

لا تغيير على المصادقة أو إبطال الجلسة أو عزل الملكية أو المخطط أو MQTT أو بوابات G0/G1/G2. الاختبارات تثبت سلوك العميل الحالي ولا تضيف عقداً.

## 7. الإغلاق

نطاق مجموعة قبول العميل **CLOSED**. تسلسل الويب بلا مهمة تالية. المؤشرات بعد المزامنة: الويب **8/8** بطاقة، والمجموع **21/50**. الجاهزية الإنتاجية ليست 100%.

## الدرس الهندسي المستفاد

أُضيف هذا القسم في 2026-11-18 أثناء `WEB-C-06` لاستكمال التوثيق. لا يغيّر لقطة **80/80** ولا زمن البناء (حوالي 395 ثانية) المسجّلين أعلاه. مجموعة الاستكمال اللاحقة وصلت إلى 121 وهي تشغيل آخر.

اختبار القبول هنا يثبت دلالات الكيان لا رسالة وسيط. `command statuses do not treat HTTP accept as device execution` في `web_acceptance_smoke_test.dart` يتحقق أن `PENDING` ليس تنفيذًا مؤكدًا، وأن `EXECUTED` وحده كذلك داخل النموذج، وأن `TIMED_OUT` يبقى حاجز تعارض حتى `reconciledAt`. ذلك يحمي الواجهة من معاملة HTTP 202 كتنفيذ جهاز. يُستخدم عندما يكون مصدر الحقيقة خارج العملية والاختبار لا يملك الجهاز. التحقق الحي يبقى `WEB-EXT-04`، ولم يُغلق G1 بهذا الملف.
