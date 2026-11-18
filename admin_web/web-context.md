# سياق المشروع — الويب (`admin_web`)

آخر تحديث: 2026-11-18  
السياق العام: [`../system-context.md`](../system-context.md) · التقدّم: [`web-progress.md`](./web-progress.md)

## الهوية

| الحقل | القيمة |
|--------|--------|
| المسؤولية | Flutter Web: شاشات المالك + لوحة الأدمن (حسابات، أجهزة، سجلات، تنبيه إداري) |
| التقنية | Flutter Web · RTL عربي · فصل مساري مالك/أدمن · بيئات · يعتمد على `packages/smart_security_app_core` |
| مسار العمل | `admin_web/` — T-01…T-08 مغلقة ضمن نطاق العميل (8/8 بلا تغيير). مرحلة الاستكمال `WEB-C-01`…`WEB-C-06` مغلقة 2026-11-18 خارج عدّاد الـ 50. الحكم البرمجي: SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING. ليست جاهزية إنتاج. `WEB-EXT-09` مكتمل. `WEB-EXT-01`…`08` في `WEB-REMAINING-EXTERNAL-WORK.md` |
| لا يملك | FCM هاتف · Wi-Fi محلي معتاد · MQTT من المتصفح · firmware |

## مبادئ ملزمة

- لا MQTT من المتصفح؛ كل التشغيل عبر HTTPS → API.
- نجاح HTTP لا يقلب الحالة المؤكدة للجهاز.
- فصل صلاحيات المالك والأدمن في التوجيه والواجهات.
- لا أسرار إنتاج في المستودع؛ إعدادات العميل العامة وحدها ليست أسراراً، لكن مفاتيح توقيع JWT وبيانات اعتماد الخادم ممنوعة في الويب.
- عقد Backend T-02 المنفذ وفق تقرير التحقق: Access JWT في الذاكرة فقط وRefresh للويب في `HttpOnly; Secure` Cookie؛ تجديد عبر HTTPS وCSRF موقّع مرتبط بالجلسة مع Origin مُتحقق منه، وحماية من التجديد المتزامن (`single-flight`). لا `localStorage`/`sessionStorage` للرموز.
- حراسة مسارات Flutter وتحجيم الأزرار تحسين تجربة مستخدم فقط؛ تفويض `ADMIN`/`OWNER` و`Users.Manage` وعزل الجهاز مسؤولية الخادم دائماً؛ الأدمن لا يتحكم افتراضياً بأجهزة المالك.
- الربط الأولي فقط `PENDING`؛ نقل الملكية ينشئ ملكية `ACTIVE` على الخادم، مع السماح للمالك الجديد بتقديم طلب تحكم عندما يصنف الخادم الجهاز متصلاً (وفق قيود الأوامر)، بينما **يمنع الخادم نشر الأمر وتنفيذه** حتى تأكيد مزامنة إصدار الملكية مع الجهاز، وإزالة بيانات المالك السابق من ذاكرة/مخزن الواجهة عند فقد التفويض.
- الأمر `PENDING` أو `TIMED_OUT` لا يساوي حالة مؤكدة؛ مصدر الحقيقة `COMMAND_ACK` بالحالة الفعلية الكاملة و`resultStateSequence`، مع بقاء اعتماد عقد G1 مطلوباً.

## ملفات التخطيط في هذا المجلد

| ملف | الدور |
|-----|--------|
| `web-context.md` | هوية، مبادئ، موانع، لقطة مهام |
| `web-progress.md` | مؤشرات + سجل زمني + نقطة تحقق |
| `web-current.md` | ورقة المهمة الجارية فقط |
| `web-development-tasks.md` | قائمة T-01…T-08 |
| `web-development-plan.md` | نطاق المراحل |

## المهمة الجارية

- المصدر التفصيلي: [`web-current.md`](./web-current.md)
- المهمة: لا تالية. `WEB-C-06` مغلقة (استكمال برمجي؛ لا تغيّر عدّ 8/8)
- الحالة: `waiting` لتوجيه المالك. ليس بدء مشروع جديد
- تقرير الإغلاق: [`../docs/reports/web/completion/WEB-C-06-report.md`](../docs/reports/web/completion/WEB-C-06-report.md)
- تقرير المرحلة: [`../docs/reports/web/completion/WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md`](../docs/reports/web/completion/WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md)
- تقرير التجاوب: [`../docs/reports/web/completion/WEB-C-05-report.md`](../docs/reports/web/completion/WEB-C-05-report.md)
- تقرير واجهة الأدمن: [`../docs/reports/web/completion/WEB-C-04-report.md`](../docs/reports/web/completion/WEB-C-04-report.md)
- تقرير واجهة المالك: [`../docs/reports/web/completion/WEB-C-03-report.md`](../docs/reports/web/completion/WEB-C-03-report.md)
- تقرير الجلسة: [`../docs/reports/web/completion/WEB-C-02-report.md`](../docs/reports/web/completion/WEB-C-02-report.md)
- تقرير المعمارية: [`../docs/reports/web/completion/WEB-C-01-report.md`](../docs/reports/web/completion/WEB-C-01-report.md)
- تقرير تسلسل T: [`../docs/reports/web/WEB-FINAL-REPORT.md`](../docs/reports/web/WEB-FINAL-REPORT.md)

## تتبّع المهام

| مهمة | العنوان | الحالة |
|------|---------|--------|
| T-01 | تأسيس Flutter Web + استهلاك shared core | مغلقة 2026-11-17 |
| T-02 | عميل API والنماذج والدخول | مغلقة 2026-11-17 (Autonomous Web) |
| T-03 | قائمة أجهزة المالك وحالة الربط | مغلقة 2026-11-17 |
| T-04 | تفاصيل المالك والأوامر | مغلقة 2026-11-17 (عميل؛ G1/ACK BLOCKED) |
| T-05 | سجل أحداث وأوامر المالك | مغلقة 2026-11-17 (عميل؛ نقل ملكية حي NOT VERIFIED) |
| T-06 | لوحة الأدمن — الحسابات والأجهزة والربط | مغلقة 2026-11-18 (عميل؛ متصفح حي NOT VERIFIED) |
| T-07 | سجلات الإدارة والتنبيه الإداري | مغلقة 2026-11-18 (عميل؛ FCM الحي NOT VERIFIED / BLOCKED) |
| T-08 | اختبارات القبول للويب | مغلقة 2026-11-18 (عميل؛ الحي CSRF/ACK/نقل ملكية/FCM/متصفح NOT VERIFIED / BLOCKED) |

**ملخص:** 8/8 بطاقات تسلسل العميل مكتملة. الجاهزية الإنتاجية ليست 100%؛ الفجوات الخارجية في `WEB-REMAINING-EXTERNAL-WORK.md`.

## قرار عقد 2026-11-16 — للواجهة لاحقاً، بلا تنفيذ Flutter الآن

- اعرض الحادثة جارية بعد الكتم، ومنتهية فقط عندما تكون الحالة المخزنة `ENDED`.
- لا تعامل نجاح HTTP كتنفيذ جهاز.
- لا شاشة تهيئة بعيدة ولا OTA في v1.

## قرارات وموانع

- مسارات مصادقة Backend T-02 وإدارة حسابات المالك منفذة بحسب نتائج Cursor؛ يتحقق فريق Web من OpenAPI الفعلي قبل الربط. عقود الأجهزة/الربط/الأوامر/التنبيهات التي لم تُنفّذ بعد لا تُفترض جاهزة وتحتاج عقد `integration` وتطبيق Backend.
- إغلاق مسارات أوامر يعتمد ACK بالحالة الفعلية من الجهاز عبر الخادم واعتماد G1؛ `D-09` محدد شكلياً ولا تعني هذه الحقيقة أن G1 أُغلقت.

## حدود تنفيذ المصادقة والتكامل

- **جاهز في Backend بحسب تقارير التنفيذ:** Login/Refresh/Logout/Logout-all/Me، وإدارة حسابات المالك (إنشاء/تعديل/تعطيل/أرشفة) مع تدقيق العمليات؛ يلزم مطابقة OpenAPI والاستجابات الفعلية عند بدء WEB-T-02/WEB-T-06.
- **منفّذ في Web:** T-01 scaffold؛ **T-02** جلسة Cookie/CSRF (عميل)؛ **T-03** قائمة أجهزة؛ **T-04** تفاصيل وأوامر المالك على `/owner/devices/:deviceId` (عميل)؛ **T-05** سجل الحوادث والأوامر على `/owner/devices/:deviceId/history` (عميل)؛ **T-06** لوحة الحسابات والأجهزة (عميل)؛ **T-07** سجل الإدارة والتنبيه الإداري على `/admin/audit-logs` و`/admin/alerts` (عميل)؛ **T-08** مجموعة قبول العميل (`test/features/acceptance/web_acceptance_smoke_test.dart` وقائمة `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`). **BLOCKED / NOT VERIFIED:** CSRF/Origin/CORS الحي، تعطيل الحساب الحي، G1/ACK وOWNERSHIP_SYNC على جهاز متصل، نقل ملكية حي، تسليم FCM الحي، والمتصفح التفاعلي ضد Backend حقيقي.
- **واجهات Backend المستهلكة هنا:** المصادقة وإدارة المالكين والأجهزة، وقراءة `GET /api/admin/audit-logs`، وإنشاء/قائمة/تفصيل `/api/admin/alerts`. حفظ التنبيه ليس نجاح تسليم FCM؛ التسليم الحي يبقى **NOT VERIFIED / BLOCKED**.

## ما أُنجز فعلياً (حتى الآن)

- استهلاك `packages/smart_security_app_core` عبر path.
- Scaffold Flutter Web مع فصل مساري المالك والأدمن، وجلسة Cookie/CSRF في الذاكرة للـ Access.
- قائمة أجهزة المالك وشاشة تفاصيل بأوامر ARM/DISARM/MUTE/TEST/STOP_TEST دون قلب المؤشر المؤكد عند HTTP 202.
- سجل حوادث وأوامر المالك مع بحث وترقيم محليين على قائمة API الكاملة، وعبارة وقوع غير مؤكد عند `clockTrusted=false` دون مدة تأخير مختلقة.
- لوحة أدمن للحسابات والأجهزة، ثم سجل إدارة وتنبيه إداري عبر HTTPS فقط. قبول الحفظ لا يُعرض كتسليم ولا كقراءة.
- مجموعة قبول برمجية تغطي حراسة مساري المالك والأدمن ودلالات أمر `PENDING`/`EXECUTED`/`REJECTED`/`TIMED_OUT` ومسح Access عند الخروج. ليست إثباتاً لـ ACK حي أو متصفح ضد خادم حقيقي.
- لا أوامر تشغيل على مسارات `/admin`. لا Firebase/FCM في الويب. لا اعتماد على `security_mobile` في البناء.

## قواعد إضافة كود Flutter

| أضف إلى الحزمة المشتركة | أبقِ في `admin_web` |
|--------------------------|---------------------|
| ثيم/أخطاء/ترجمة عامة/ويدجتات | Cookie refresh + CSRF، Router مالك/أدمن، شاشات الأدمن والإدارة |
| لا تكرار تأسيس الحزمة هنا | لا تعتمد على `security_mobile` |

## قواعد تحديث هذا الملف

1. عند بدء/إنهاء مهمة: حدّث الجدول والملخص، وزامن `web-current.md`.
2. عند فصل مسارات مالك/أدمن أو عقد API: سجّل القرار هنا.
3. لا تكرر تفاصيل القبول هنا.
4. بعد تحديث جوهري: انسخ الملخص إلى [`../system-context.md`](../system-context.md).
