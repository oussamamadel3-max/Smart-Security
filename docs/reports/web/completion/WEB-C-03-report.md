# تقرير WEB-C-03 — Owner UI & Design Fidelity Audit

| الحقل | القيمة |
|--------|--------|
| المهمة | `WEB-C-03` |
| الحالة | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` فقط. الحزمة المشتركة لم تُمس. لا تعديل على Backend / mobile / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` خارج النطاق |
| العدّاد | لا تغيير على 8/8 ولا على 21/50 |
| المرجع البصري | `design/index.html` (وضع المالك) |
| المرجع السلوكي | SRS والعقود المنفذة. نموذج HTML التجريبي (دخول جاهز، تبديل دور، ACK مزيّف) لم يُنقل |

## 1. النطاق والاعتماديات

مطابقة شاشات المالك الموجودة: الدخول، الرئيسية/قائمة الأجهزة، بطاقة الجهاز، التفاصيل، حالات الاتصال والحماية والصفارة والعتاد، الربط `PENDING`، قوائم وتفاصيل الحوادث والأوامر، الإشعارات النصية، والحالات الفرعية. لا لوحة جانبية جديدة ولا شاشة لوحة مؤشرات أو إشعارات أو حساب منفصلة، لأن مسارات الإنتاج هي `GoRouter` الحالية والشريط الجانبي في HTML يتضمن مبدّل أدوار تجريبياً.

الاعتماد: `WEB-C-01` و`WEB-C-02` مغلقتان.

## 2. الملفات

| ملف | التغيير |
|------|---------|
| `admin_web/lib/core/widgets/owner_visual.dart` | جديد. عناوين التصميم، عرض الدخول 475، عرض المحتوى 1650، فاصل 780/1100، ترويسة الصفحة، علامة الحصن |
| `admin_web/lib/features/auth/presentation/pages/login_page.dart` | ترويسة ومحاذاة RTL وعرض 475 وزر بعرض اللوحة. الحقول تبقى فارغة و`userName` |
| `admin_web/lib/features/devices/presentation/pages/devices_list_page.dart` | ترويسة، إشعار الربط، شبكة عمودين فوق 780 |
| `admin_web/lib/features/devices/presentation/pages/device_detail_page.dart` | بطل مع الدرع، أربع بطاقات حالة، لوحة أوامر |
| `admin_web/lib/features/devices/presentation/pages/device_history_page.dart` | ترويسة، رأس جدول، شارات الحالة، رابط التفاصيل |
| `admin_web/lib/features/devices/presentation/pages/alarm_event_detail_page.dart` | شبكة خصائص وشارة التصنيف |
| `admin_web/lib/features/devices/presentation/pages/command_detail_page.dart` | شبكة خصائص وشارة الأمر |
| `admin_web/lib/features/devices/presentation/widgets/device_card.dart` | نصف قطر 13 وأيقونة 41. بطاقة `PENDING` بلا فقرة التحكم |
| `admin_web/lib/features/devices/presentation/widgets/device_status_pills.dart` | أزرق الشارة = `infoSoft`، ورمادي أقرب رمز سطح، وشارات الأمر/الحادثة |
| `admin_web/lib/features/devices/presentation/widgets/device_command_bar.dart` | جديد. ARM أساسي، DISARM ثانوي، الباقي محدد |
| `admin_web/lib/features/devices/presentation/widgets/state_metric_card.dart` | جديد. شبكة `.metric` |
| `admin_web/lib/features/devices/presentation/widgets/info_notice.dart` | خلفية `sky` وارتفاع سطر 1.9 |
| `admin_web/lib/features/devices/presentation/widgets/history_kv_row.dart` | خلايا `#f7f9fc` وشبكة عمودين |
| `admin_web/test/features/devices/owner_visual_fidelity_test.dart` | جديد |
| `admin_web/tool/owner_visual_preview.dart` | معاينة ثابتة خارج الموجّه. ليست تجاوزاً لمصادقة الإنتاج |

لا أثر على مخطط قاعدة البيانات ولا على عقود MQTT/HTTP ولا على مكان حفظ Access.

## 3. خريطة التصميم إلى Flutter

| قسم `design/index.html` | المسار | الحكم |
|-------------------------|--------|--------|
| `owner('login')` | `/login` | طُبّق بصرياً مع إبقاء `userName` فارغاً |
| `owner('devices')` والبطاقة `.device` | `/owner` | الرئيسية الإنتاجية هي قائمة الأجهزة لا لوحة التحية |
| `owner('detail')` | `/owner/devices/:deviceId` | بطاقات الحالة ولوحة الأوامر |
| `owner('events')` / `eventDetail` | `.../history` و`.../history/events/:alarmEventId` | جدول وشبكة خصائص |
| `owner('commands')` / `commandDetail` | `.../history?tab=commands` و`.../history/commands/:commandId` | شارات الحالة. لا سجل انتقال مزيّف |
| `owner('dashboard'/'notifications'/'profile')` | غير مسجّلة | صفحات نموذج. لم تُنشأ |
| الشريط الجانبي ومبدّل المالك/الإدارة/الهاتف | غير منقول | تجريبي. غلاف الأدمن يبقى لمهمة `WEB-C-04` |
| نوافذ التأكيد في HTML | لا حوار مالك للتشغيل | الأوامر تُرسل مباشرة كما في السلوك المعتمد. الثيم يستخدم لون `toast` |

## 4. مصفوفة المقارنة

| الشاشة | المسار | مرجع التصميم | الفرق الذي عُولج | ما بقي | الدليل | الحكم |
|--------|--------|----------------|------------------|--------|--------|--------|
| الدخول | `/login` | `.login-view` عرض 475، ترويسة، لوحة، زر كامل | كان العنوان في الوسط والعرض 420 | البريد التجريبي لم يُملأ. الحقل اسم مستخدم وفق العقد | `evidence/owner/owner-login.png` | VERIFIED للإطار الملتقط |
| قائمة الأجهزة | `/owner` | `.page-head` + `.notice.info` + شبكة `.devices` | عمود واحد دائماً وإشعار بلون شارة | لا شريط جانبي | `owner-devices.png` فوق 780، و`owner-devices-narrow.png` عند 727 | VERIFIED |
| بطاقة الجهاز | داخل `/owner` | نصف قطر 13، أيقونة ⌂، شارات | نصف القطر كان 16 | تدرج الحوم `#87bbe7` لم يُقاس | اللقطتان | VERIFIED |
| الربط `PENDING` | البطاقة + الإشعار | الاسم وحالة الربط فقط | الفقرة الطويلة كانت تتكرر داخل البطاقة | — | بطاقة «البوابة» في `owner-devices.png` | VERIFIED |
| التفاصيل | `/owner/devices/:deviceId` | `.detail-hero` + أربع `.metric` + `.command-row` | الشارات كانت بدل البطاقات، وكتم الإنذار زراً أساسياً | التدرج يستخدم `navy`/`navy2` لا `#143c61`/`#10243e` | اختبار الويدجت. الإطار تحت الطية لم يُلتقط | PARTIAL بصرياً |
| الاتصال / الحماية / الصفارة / العتاد | بطاقات التفاصيل | `.metric` | نُقلت إلى `StateMetricCard` | رمز المقياس `infoSoft`/`primary` لا `#eef6ff`/`#2b7dc3`. القيمة الطويلة تصغر إلى 18 | `state metrics keep the four design titles` | PARTIAL |
| الحوادث | تبويب السجل | `.table` + `.pill` | رأس `#f7f9fc` وشارة التصنيف | لا لقطة متصفح | الكود | NOT VERIFIED بصرياً |
| تفاصيل الحادثة | مسار الحادثة | `.kv` + إشعار التأخر | خلايا رمادية وشبكة وشارة | — | الكود | NOT VERIFIED بصرياً |
| الأوامر | تبويب الأوامر | `.pill` للحالة | «منفذ / قيد التنفيذ / انتهت المهلة / مرفوض» | رموز العقد تبقى في صف الخصائص | الكود + `commandStatus` | NOT VERIFIED بصرياً |
| تفاصيل الأمر | مسار الأمر | لوحة + إشعار `TIMED_OUT` | شارة وشبكة | خطوات الانتقال في HTML بيانات تجريبية بلا سلسلة في الكيان | الكود | PARTIAL |
| الإشعار info/warning | القوائم والتفاصيل | `.notice` / `.notice.info` | info كان لون الشارة الزرقاء | حد info يبقى `border` لا `#d9eaf8` | الإشعار الأزرق في `owner-devices.png` | VERIFIED للنص الظاهر |
| حوارات المالك | — | لا حوار تشغيل في وضع المالك | لم يُضف حوار تجريبي | SnackBar يستخدم `toastBg` من الثيم السابق | قراءة الثيم | VERIFIED كغياب للحوار المزيّف |

بقايا اللون المقصودة لأنها أقرب رموز موجودة: الشارة الرمادية `AppColors.lightSurfaceVariant` (`#F7F9FC`) بدل `#F0F3F7`، ونصها `muted` (`#79899B`) بدل `#6E8196`.

## 5. مصفوفة القبول

| البند | الحكم | الدليل |
|--------|--------|--------|
| مراجعة شاشات المالك مقابل التصميم | VERIFIED | القسم 3 و4 |
| تصحيح الانحراف غير المبرر داخل الرموز | VERIFIED | الملفات أعلاه |
| عدم نقل الدخول التجريبي | VERIFIED | `login keeps the design width and empty fields` يرفض `ahmad@example.test` |
| `PENDING` بلا تحكم | VERIFIED | `pending card keeps link status and hides arm controls` واللقطة |
| ألوان الشارات الزرقاء/الرمادية | VERIFIED | `blue and gray pills use design token fills` |
| زر التعطيل الثانوي | VERIFIED | `command bar uses primary arm and soft disarm` يقرأ `primarySoft` |
| فاصل 780 و1100 | VERIFIED | `owner layout breakpoints follow design/index.html` |
| معاينة متصفح للدخول والقائمة | VERIFIED للإطارين | اللقطات أدناه. بيانات المعاينة ثابتة وليست جلسة إنتاج |
| معاينة متصفح للتفاصيل والسجل | NOT VERIFIED | أداة المتصفح لم تحرّك `ListView` الداخلي تحت الطية |
| analyze / test / build | VERIFIED | القسم 6 |
| G0 / G1 / G2 ومطابقة بكسل | NOT VERIFIED | خارج النطاق. لا ادّعاء تطابق بكسل |

## 6. الأوامر

من `admin_web`:

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` | **No issues found** (47.0s) |
| `flutter test` | **99/99** نجحت. منها 7 في `owner_visual_fidelity_test.dart` |
| `flutter build web --release` | **exit 0** · `Built build\web` · تجميع 859.5s · المدة الكلية ~922s. تحذير Wasm dry run نجح (لم يُبنَ `--wasm`). أيقونات Cupertino/Material شُذّبت. المخرج `admin_web/build/web/index.html` موجود |
| `flutter run -d chrome -t tool/owner_visual_preview.dart --web-port=7357` | خدم المعاينة. الالتقاط من متصفح الأداة على `http://127.0.0.1:7357` |

اللقطات:

- `docs/reports/web/evidence/owner/owner-login.png`
- `docs/reports/web/evidence/owner/owner-devices.png`
- `docs/reports/web/evidence/owner/owner-devices-narrow.png` (عرض مقاس 727 عبر CDP، عمود واحد)

المعاينة ليست مسار إنتاج ولا تتجاوز الحارس.

## 7. المخاطر المتبقية

- تدرج بطل التفاصيل وحد إشعار info ورمادي الشارة ما زالت أقرب الرموز لا القيم الحرفية في CSS.
- السجل وتفاصيل الحادثة/الأمر بلا لقطة متصفح في هذه الجولة.
- لا شريط جانبي للمالك. غلاف الأدمن يُراجع في `WEB-C-04`.
- لا ادّعاء بإغلاق G1 أو بمعاينة ضد Backend حي.

## 8. الحكم

**CLOSED** لنطاق البرمجيات في `WEB-C-03`. التالية: `WEB-C-04`.

## 🧠 مقتطف هندسي من المهمة

`.pill.blue` في `design/index.html` هو `#e9f3ff` / `#286bb1`، وهما `DesignTokens.infoSoftBg` و`infoSoftText`. `.btn.secondary` لون آخر: `primarySoft` `#eaf2fc` و`primaryTextSoft` `#1763ad`. الشارة الزرقاء كانت تأخذ لون الزر الثانوي، فبدت حالة «الحماية مفعّلة» كزر لا كتصنيف. التصحيح في `StatusPill` يربط `StatusPillTone.blue` برمزي info، و`DeviceCommandBar` يضع خلفية `primarySoft` على تعطيل الحماية فقط. يُستخدم الفصل كلما اشترك لونان ناعمان في عائلة واحدة وكان أحدهما شارة والآخر زراً. التحقق: `blue and gray pills use design token fills` يتأكد أن تعبئة الأزرق هي `infoSoftBg` وليست `primarySoft`، و`command bar uses primary arm and soft disarm` يتأكد أن خلفية زر التعطيل هي `primarySoft`.
