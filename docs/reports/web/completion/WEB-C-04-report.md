# تقرير WEB-C-04 — Admin UI & Design Fidelity Audit

| الحقل | القيمة |
|--------|--------|
| المهمة | `WEB-C-04` |
| الحالة | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` فقط. الحزمة المشتركة لم تُمس في هذه المهمة. لا تعديل على Backend / mobile / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` خارج النطاق |
| العدّاد | لا تغيير على 8/8 ولا على 21/50 |
| المرجع البصري | `design/index.html` دالة `admin()` |
| المرجع السلوكي | SRS والعقود المنفذة. مبدّل الأدوار والدخول التجريبي وACK المزيّف وصفحة الأعطال بلا API لم تُنقل |

## 1. النطاق والاعتماديات

مطابقة شاشات الإدارة الموجودة: لوحة المؤشرات، الشريط والرأس، الحسابات (قائمة/إنشاء/تفاصيل/تعطيل/أرشفة)، الأجهزة (قائمة/تسجيل/تفاصيل)، الربط والنقل وإلغاء الربط والأرشفة، سجل الإدارة وتفاصيله، التنبيه الإداري ونتيجته، والحوارات وحالات التحقق. لا أوامر `ARM` / `DISARM` على مسارات `/admin`.

الاعتماد: `WEB-C-01`…`WEB-C-03` مغلقة. فاصل الشريط الجانبي يبقى `960` (درج تحته). المرور الكامل على 360…1920 هو `WEB-C-05`.

## 2. الملفات التي وحّدت الواجهة

| ملف | التغيير |
|------|---------|
| `admin_web/lib/core/widgets/owner_visual.dart` | `OwnerPageHeader` يقبل `eyebrow` و`section` حتى يعيد الأدمن الترويسة دون سحب ويدجتات شاشات المالك |
| `admin_web/lib/features/admin/presentation/admin_copy.dart` | حاجب «نظام الأمان الذكي / الإدارة»، فتات، نصوص المرشحات، وخطوات الملكية. مفاتيح `fieldMessage` السابقة بقيت |
| `admin_web/lib/features/admin/presentation/widgets/admin_widgets.dart` | طقم مشترك: إشعار، حقل بخطأ، جدول، بحث، مرشح، ترقيم، خطوات، خط زمني، بطاقات، وأزرار، وحوار تأكيد |
| `admin_web/lib/features/admin/presentation/widgets/admin_shell.dart` | شريط ≥960، رأس بارتفاع 78 فوق 780 و65 تحته، فتات المسار، عنصر التنقل المختار `DesignTokens.navy2` مع حد `brandMarkStart`، وتذييل `AdminCopy.noOwnerCommands` |
| صفحات `features/admin/presentation/pages/` | اللوحة، الحسابات، الإنشاء، التفاصيل، الأجهزة، التسجيل، تفاصيل الجهاز، الملكية، السجل، التنبيهات |
| `admin_web/lib/features/admin/presentation/widgets/admin_alert_outcome_panel.dart` | نتيجة الحفظ/التسليم عبر `AdminKvGrid` |
| `admin_web/test/features/admin/admin_visual_fidelity_test.dart` | 8 اختبارات للملاحة والجدول والتحقق والحوار واللوحة والربط وغياب أوامر المالك |
| `admin_web/tool/admin_visual_preview.dart` | معاينة ثابتة خارج موجّه الإنتاج. ليست تجاوزاً لمصادقة الإنتاج |

`MyDialog` في الحزمة المشتركة بقي كما هو. حوار التأكيد الإداري محلي: `showAdminConfirmDialog` بعرض 490 ونصف قطر 18.

لا أثر على مخطط قاعدة البيانات ولا على عقود MQTT/HTTP ولا على مكان حفظ Access.

## 3. خريطة التصميم إلى Flutter

| قسم `design/index.html` | المسار | الحكم |
|-------------------------|--------|--------|
| `admin('dashboard')` | `/admin` | أربع بطاقات من بيانات الحسابات والأجهزة. لا عدّ تنبيهات مخترع |
| `admin('accounts')` / `addAccount` / `accountDetail` | `/admin/accounts` و`/new` و`/:ownerId` | جدول وبحث ومرشح ونموذج. الدور يبقى `OWNER` نصاً بلا قائمة أدمن تجريبية |
| `admin('adminDevices')` / `register` / `adminDeviceDetail` | `/admin/devices` ومساراتها | شارات اتصال/ملكية. الحساس للقراءة «اهتزاز». لا قيم `D-03` مخمّنة |
| الربط / النقل / إلغاء الربط | `.../link` و`.../transfer` وأزرار التفاصيل | إشعارات الملكية وخطوات المسار. لا زر تشغيل |
| `admin('audit')` | `/admin/audit-logs` و`/:logId` | جدول وترقيم. التفاصيل من الصف تعرض الشبكة. الرابط العميق بلا صف في الجلسة يعرض حالة «غير متاح» |
| `admin('alerts')` | `/admin/alerts` و`/new` و`/:alertId` | خط زمني ونموذج ونتيجة حفظ/تسليم |
| `admin('faults')` ومبدّل الأدوار | غير مسجّلة | لا API / سلوك تجريبي. لم يُنشآ |
| `.modal` | `showAdminConfirmDialog` | عُرض في تعطيل الحساب |

بقايا CSS لم تُضف كرموز جديدة: خمول التنقل `#BED0E2`، وتسمية الشريط `#7591AE`، وحد التذييل `#31506D`. العنصر النشط في CSS هو `#234360`؛ البديل المستخدم هو `DesignTokens.navy2`. رأس الجدول الرمادي هو `AppColors.lightSurfaceVariant` (`#F7F9FC`).

## 4. مصفوفة المقارنة

إطار المتصفح الذي التُقط فعلاً هو **548×594**. هذا تحت فاصل الشريط `960` وتحت فاصل التصميم `780`، لذلك الشريط الجانبي درج مغلق والصورة تظهر الرأس وزر القائمة لا العمود `256`. محاولة `Page.captureScreenshot` مع عرض 1440 كرّرت الإطار نفسه ولم تُحفظ كدليل. لا ادّعاء تطابق بكسل.

المعاينة: `flutter run -d web-server -t tool/admin_visual_preview.dart --web-port=7358`. الصورة الرمزية «م» لأن المعاينة بلا جلسة. البيانات ثابتة.

| الشاشة | المسار | مرجع التصميم | ما ظهر في الدليل | ما بقي | الدليل | الحكم |
|--------|--------|----------------|------------------|--------|--------|--------|
| اللوحة | `/admin` | `.page-head` + `.metric` + جدولان | حاجب، عنوان، 4 بطاقات (3/2/1/1)، جدول أجهزة وحسابات، زر تسجيل | الشريط العريض غير ظاهر في هذا العرض | `admin-dashboard.png` | VERIFIED للإطار 548 |
| الرأس والفتات | كل الصفحات | ارتفاع الشريط وفتات `الرئيسية / …` | فتات عربي وزر قائمة وزر خروج. الارتفاع 78 لم يُقاس عند 548 | درج القائمة لم يُفتح بالنقر | اللقطات + اختبار `navy2` عند 1440 | PARTIAL للشريط العريض |
| الحسابات | `/admin/accounts` | `.table` + بحث + مرشح + `.pill` | إضافة حساب، بحث، «كل الحالات»، شارات نشط/مالك/معطّل | أعمدة الجدول تحتاج تمرير أفقي تحت 650 | `admin-accounts.png` | VERIFIED للمحتوى الظاهر |
| إنشاء حساب | `/admin/accounts/new` | نموذج بعرض ~720–750 | حقول ورجوع وحفظ. الدور `OWNER` | أزرار الحفظ تحت الطية في 548 | `admin-account-new.png` | VERIFIED |
| خطأ التحقق | الإنشاء فارغاً | حد الحقل لا إشعار الصفحة | حد أحمر ونص «اسم المستخدم مطلوب…» على الحقل الأول فقط | بقية الحقول بلا رسالة حتى يدور المفتاح | `admin-account-new-validation.png` | VERIFIED |
| تفاصيل الحساب | `/admin/accounts/owner-1` | `.kv` + أزرار خطر | شبكة وشارة نشط | أزرار التعديل/التعطيل/الأرشفة تحت الطية في اللقطة العلوية | `admin-account-detail.png` | VERIFIED للشبكة |
| حوار التعطيل | الحوار | `.modal` عرض 490 | عنوان، إشعار، إلغاء، تأكيد بخلفية ناعمة | حوار إلغاء الربط نفسه بلا لقطة منفصلة؛ نفس `showAdminConfirmDialog` | `admin-account-disable-dialog.png` | VERIFIED لهذا الحوار |
| الأجهزة | `/admin/devices` | جدول + إشعاران | تحذير عدم التشغيل وإشعار المتابعة، شارات متصل/`ACTIVE` و`PENDING` | صف `PENDING` مقطوع أسفل الإطار | `admin-devices.png` | VERIFIED |
| تسجيل جهاز | `/admin/devices/register` | نموذج + حساس للقراءة | إشعار `D-03`، حساس «اهتزاز»، `PASSWORD_HASH` | لا قيم مهلة مخترعة | `admin-device-register.png` | VERIFIED |
| تفاصيل الجهاز | `/admin/devices/device-1` | خصائص بلا أوامر مالك | شارة ملكية، إشعاران، حماية/صفارة/عتاد كنص | لا أزرار `ARM`/`DISARM` | `admin-device-detail.png` و`admin-device-detail-actions.png` | VERIFIED |
| أزرار الملكية | أسفل التفاصيل | أساسي/ثانوي/خطر/محدد | نقل، إلغاء ربط، أرشفة، سجلات | جهاز `ACTIVE` لا يعرض زر الربط الأولي | `admin-device-detail-buttons.png` | VERIFIED |
| النقل | `.../device-1/transfer` | إشعار + منتقي + `.steps` | ثلاث خطوات: تسجيل، انتظار `PENDING`، ملكية `ACTIVE` | — | `admin-device-transfer.png` | VERIFIED |
| الربط | `.../device-2/link` | `PENDING` ليست تشغيلاً | نفس هيكل الخطوات مع نص الربط الأولي | — | `admin-device-link.png` | VERIFIED |
| السجل | `/admin/audit-logs` | `.table` + ترقيم | بحث، تطبيق، `AUD-112` نجاح | عمود التفاصيل خارج الإطار حتى التمرير الأفقي | `admin-audit.png` و`admin-audit-scrolled.png` | VERIFIED |
| تفاصيل السجل | من الصف `AUD-112` | `.kv` | شارة نجاح وشبكة العملية | الرابط العميق بلا صف يعرض «السجل غير متاح في هذه الجلسة» | `admin-audit-detail-loaded.png` و`admin-audit-detail.png` | VERIFIED للحالتين |
| التنبيهات | `/admin/alerts` | `.timeline` + ترقيم | بطاقة «المستودع» وحالة معلّق وصفحة 1 | — | `admin-alerts.png` | VERIFIED |
| إنشاء تنبيه | `/admin/alerts/new` | نموذج | مستلم/عنوان/نص. الخطأ على المستلم: «معرف المستلم يجب أن يكون GUID» | — | `admin-alert-new.png` و`admin-alert-validation.png` | VERIFIED |
| نتيجة التنبيه | `/admin/alerts/alert-1` | حفظ ≠ تسليم | «قُبل حفظ… ليس نجاح تسليم» و`PENDING` | التسليم الحي FCM يبقى خارج النطاق | `admin-alert-result.png` | VERIFIED |
| صفحة الأعطال ومبدّل الدور | — | موجودة في HTML التجريبي | لم تُبنَ | بلا عقد | — | متعمّد |

## 5. مصفوفة القبول

| البند | الحكم | الدليل |
|--------|--------|--------|
| مراجعة شاشات الأدمن مقابل التصميم | VERIFIED | القسمان 3 و4 |
| توحيد الغلاف والجداول والنماذج والحوار داخل الرموز | VERIFIED | `admin_widgets.dart` و`admin_shell.dart` |
| خطأ التحقق على الحقل لا كإشعار عام | VERIFIED | اللقطتان + `create owner pins validation on the userName field` |
| حوار التأكيد بعرض التصميم | VERIFIED | `confirm dialog uses the design width and danger fill` ولقطة التعطيل |
| رأس الجدول بلون السطح | VERIFIED | `admin table header uses the design surface` |
| غياب أوامر المالك | VERIFIED | اللقطات + `dashboard keeps admin scope and hides owner commands` و`device detail still has no owner command buttons` |
| عنصر التنقل المختار `navy2` لا `primary` | VERIFIED في الاختبار عند 1440 | `selected admin nav uses navy2 rather than primary`. بلا لقطة متصفح للشريط المفتوح |
| درج العرض 548 | NOT VERIFIED | أيقونة القائمة ظاهرة. النقر الإحداثي لم يفتح الدرج |
| معاينة 1440 في المتصفح | NOT VERIFIED | الالتقاط العريض تكرّر الإطار ولم يُعتمد |
| analyze / test / build | VERIFIED | القسم 6 |
| G0 / G1 / G2 ومطابقة بكسل وFCM الحي | NOT VERIFIED | خارج النطاق |

## 6. الأوامر

من `admin_web`:

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` | **No issues found** (100.7s) |
| `flutter test` | **107/107** نجحت، exit 0. منها 8 في `admin_visual_fidelity_test.dart` |
| `flutter build web --release` | **exit 0** · `Built build\web` · تجميع `lib\main.dart` ‏509.1s · المدة الكلية ~573s. تحذير Wasm dry run (لم يُبنَ `--wasm`). أيقونات Cupertino/Material شُذّبت |

المعاينة التي أُخذت منها اللقطات: `flutter run -d web-server -t tool/admin_visual_preview.dart --web-hostname=127.0.0.1 --web-port=7358`. أُوقف الخادم قبل البناء. اللقطات تحت `docs/reports/web/evidence/admin/`.

## 7. المخاطر المتبقية

- الشريط العريض والدرجات 360…1920 لم تُلتقط. `WEB-C-05` تملك overflow والدرج.
- `#234360` لم يُضف كرمز؛ `navy2` بديل مقصود وليس قياساً بكسل.
- المعاينة بيانات ثابتة بلا جلسة إنتاج. لا ادّعاء بمصادقة حية أو CSRF حي.
- الرابط العميق لسجل غير موجود في ذاكرة القائمة يعرض حالة صريحة لا شبكة فارغة مزيفة.

## 8. الحكم

**CLOSED** لنطاق البرمجيات في `WEB-C-04`. التالية: `WEB-C-05`.

## 🧠 مقتطف هندسي من المهمة

خطأ النموذج الإداري يُعلَّق على مفتاح الحقل في `AdminWriteState.field`. `adminFieldError` في `admin_widgets.dart` يعيد `AdminCopy.fieldMessage` فقط عندما يساوي المفتاح اسم الحقل، فيتلون «اسم المستخدم» أو «المستلم» وحدهما. إذا وصل خطأ و`field == null` يذهب إلى `AdminNotice` بدرجة danger حتى لا يُلصق برسالة حقل. يُستخدم الفصل عندما يعيد الإرسال أول مفتاح ناقص ويبقي خطأ الخادم عاماً. التحقق: `admin-account-new-validation.png` يُظهر حد اسم المستخدم فقط، واختبار `create owner pins validation on the userName field` يثبت المفتاح.
