# تقرير WEB-C-05 — Responsive Design & User Experience Audit

| الحقل | القيمة |
|--------|--------|
| المهمة | `WEB-C-05` |
| الحالة | **CLOSED** (إغلاق تلقائي — Autonomous Sequential Web Software Completion) |
| التاريخ | 2026-11-18 |
| النطاق | `admin_web` فقط. الحزمة المشتركة لم تُمس. لا تعديل على Backend / mobile / ESP / ops / SRS / عقود `integration` |
| البوابات | `G0` / `G1` / `G2` خارج النطاق |
| العدّاد | لا تغيير على 8/8 ولا على 21/50 |
| التالية | `WEB-C-06` — لم تُبدأ في هذا الوكيل |

## 1. النطاق والاعتماديات

تدقيق التجاوب وحالات الواجهة والتنقل وإمكانية الوصول وأداء الإبقاء على الموارد داخل `admin_web`. الاعتماد: `WEB-C-01`…`WEB-C-04` مغلقة. لا ادّعاء لقطة متصفح لهذه الجولة.

الأحجام التي ضُخت في اختبار الويدجت: **360، 390، 768، 1024، 1440، 1920**، مع مقياس نص **1.5** عند 360. حوار التأكيد فُحص عند **360×640**.

## 2. الشدة وما أُصلح

| الشدة | الفجوة | الإصلاح |
|--------|--------|---------|
| عالية | زر الإجراء في `OwnerPageHeader` يبقى في صف مع العنوان، فيُزاح أو يفيض تحت ~560 | تحت 560 يُكدَّس الزر تحت النص ويبقى داخل الإطار |
| عالية | معرّفات بلا مسافات تفيض خلايا `AdminTable` | حد أدنى 132 لكل عمود مع تمرير أفقي، و`maxLines: 2` مع ellipsis |
| عالية | انتهاء الجلسة يفتح الدخول بلا سبب ظاهر | شريط «انتهت الجلسة» من نصوص l10n الموجودة عندما تكون الحالة `sessionExpired` فقط |
| عالية | مسار غير مسجّل بلا صفحة عربية | `NotFoundPage` عبر `errorBuilder`. الزائر غير المصادق يُحوَّل للدخول قبلها |
| متوسطة | ترقيم الإدارة وصفّ أزرار الحوار يفيضان على العرض الضيق | `AdminPager` يتكدس تحت 520. الحوار يتمرّر والأزرار في `Wrap` |
| متوسطة | الطابع الزمني في الخط الزمني ومعرّف الجهاز في بطاقة المالك | تكديس تحت 480 للزمن، وellipsis لمعرّف الجهاز واسمه، وتصغير الرمز تحت 420 |
| متوسطة | فتات الشريط وشبكة المؤشرات تستخدم عرض النافذة لا عرض المحتوى | ellipsis لسطرين. أعمدة المؤشرات من `LayoutBuilder` |
| متوسطة | مغادرة إنشاء حساب أو تسجيل جهاز أو إنشاء تنبيه تمسح الحقول بلا تنبيه | `AdminLeaveGuard` + حوار التأكيد. الإلغاء يُبقي النموذج |
| متوسطة | تبويب سجل المالك لا يحدّث `?tab=` | `context.go` عند استقرار التبويب، و`didUpdateWidget` يعيد المؤشر مع الرجوع |
| منخفضة | زر الحفظ المشغول يعرض `...` | `AdminCopy.busy` = «جارٍ التنفيذ» |
| منخفضة | رابط «التفاصيل» كان أقصر من هدف لمس مريح | حد أدنى 44×32 |
| منخفضة | خطوة المسار بلا حد أقصى للعرض | `maxWidth: 240` حتى يلتف `Wrap` |

لم يُضف هيكل عظمي (skeleton) جديد. التحميل يبقى `CircularProgressIndicator` أثناء الطلب فقط، ويُستبدل بفراغ أو خطأ أو محتوى عند انتهاء الطلب. لا تُعرض بيانات قديمة كأنها جديدة بعد فقد الجلسة: مسار الجلسة السابق ما زال يفرّغ القوائم عند فقد المصادقة، والاختبارات القديمة لذلك ما زالت خضراء.

## 3. الملفات

| ملف | التغيير |
|------|---------|
| `admin_web/lib/core/widgets/owner_visual.dart` | تكديس إجراء الرأس تحت 560 |
| `admin_web/lib/features/admin/presentation/widgets/admin_widgets.dart` | جدول، ترقيم، حوار، خط زمني، شبكة، حارس مغادرة |
| `admin_web/lib/features/admin/presentation/widgets/admin_shell.dart` | فتات بسطرين |
| `admin_web/lib/features/admin/presentation/admin_copy.dart` | `busy` ونص مغادرة النموذج |
| `admin_web/lib/features/admin/presentation/pages/create_owner_page.dart` | حارس مغادرة ونص الانشغال |
| `admin_web/lib/features/admin/presentation/pages/register_device_page.dart` | حارس مغادرة ونص الانشغال |
| `admin_web/lib/features/admin/presentation/pages/admin_alerts_page.dart` | حارس مغادرة لإنشاء التنبيه |
| `admin_web/lib/features/admin/presentation/pages/ownership_pages.dart` | نص الانشغال للربط والنقل |
| `admin_web/lib/features/admin/presentation/pages/owner_account_detail_page.dart` | نص الانشغال للحفظ |
| `admin_web/lib/features/auth/presentation/pages/login_page.dart` | خطأ الحقل وشريط الجلسة |
| `admin_web/lib/features/auth/presentation/viewmodels/login_view_model.dart` | مفتاح `userName` / `password` |
| `admin_web/lib/features/devices/presentation/pages/device_detail_page.dart` | بطاقة الجهاز تضيق وتقص المعرّف |
| `admin_web/lib/features/devices/presentation/pages/device_history_page.dart` | خلايا وترحيل ومزامنة التبويب |
| `admin_web/lib/features/devices/presentation/widgets/device_card.dart` | اسم الجهاز بسطرين |
| `admin_web/lib/features/devices/presentation/widgets/state_metric_card.dart` | أعمدة من عرض المحتوى |
| `admin_web/lib/features/bootstrap/presentation/pages/not_found_page.dart` | صفحة 404 عربية |
| `admin_web/lib/core/router/app_router.dart` | `errorBuilder` |
| `admin_web/test/features/responsive/responsive_ux_test.dart` | 14 اختباراً للعرض والحالات والتنقل |

لا أثر على مخطط قاعدة البيانات ولا على عقود MQTT/HTTP ولا على مكان حفظ Access.

## 4. مصفوفة القبول

| البند | الحكم | الدليل |
|--------|--------|--------|
| لوحة الأدمن عند 360 / 390 / 768 / 1024 / 1440 / 1920 بلا overflow وزر التسجيل داخل الإطار | VERIFIED | `admin dashboard fits …` و`_expectOnScreen` |
| درج القائمة تحت 960 والشريط عند 960 فأكثر | VERIFIED في اختبار الويدجت | النقر على تلميح «القائمة» يُظهر «حِصن»؛ عند 1024+ الدرج `null` |
| مقياس النص 1.5 عند 360 | VERIFIED | `admin dashboard survives 1.5 text scale at 360` |
| جدول بمعرّف طويل عند 360 | VERIFIED | `long table ids scroll instead of overflowing at 360` |
| حوار التأكيد داخل 360×640 | VERIFIED | `confirm dialog stays inside a 360x640 viewport` |
| تحقق الدخول على حقل اسم المستخدم | VERIFIED | `login pins an empty username on the field` |
| شريط انتهاء الجلسة، وغيابه في الدخول العادي | VERIFIED | `session expiry is visible…` و`findsNothing` في اختبار الدخول |
| تحذير مغادرة نموذج إنشاء الحساب | VERIFIED | `dirty create-owner form warns before leaving` |
| مزامنة تبويب السجل مع `?tab=commands` عند 390 | VERIFIED | `history tab keeps the route query in sync` |
| 404 عربي لمسار `/missing-web-page` مع جلسة أدمن | VERIFIED | `unknown route renders the arabic not-found page` |
| صفوف سجل المالك الممتلئة عند ≥900 | PARTIAL | الكروم الفارغ عند 390 بلا استثناء. القص مضاف في `_DataRow` ولم يُضخ صف GUID في هذا الاختبار |
| لقطات متصفح تحت `docs/reports/web/evidence/responsive/` | NOT VERIFIED | لم يُلتقط إطار متصفح في هذه الجولة. الدليل هو سطح اختبار الويدجت |
| analyze / test / build | VERIFIED | القسم 5 |
| CSRF الحي، المتصفح ضد Backend، G0/G1/G2 | NOT VERIFIED | خارج النطاق |

تعديل حساب موجود، واختيار المالك في الربط/النقل، لا يعرضان تحذير مغادرة. نماذج كلمة المرور والاعتماد والتنبيه هي التي تحذّر.

## 5. الأوامر

من `admin_web`:

| الأمر | النتيجة |
|--------|---------|
| `flutter pub get` | **exit 0** · `Got dependencies!` · 23 حزمة أحدث خارج القيود ولم تُرفع |
| `flutter analyze` | **No issues found** (61.0s) · exit 0 |
| `flutter test` | **121/121** نجحت · exit 0 · عدّاد النهاية `01:03 +121` · منها 14 في `responsive_ux_test.dart`. كانت 107/107 في WEB-C-04 |
| `flutter build web --release` | **exit 0** · `Built build\web` · تجميع `lib\main.dart` ‏155.6s. تحذير Wasm dry run (لم يُبنَ `--wasm`). أيقونات Cupertino/Material شُذّبت |

`flutter pub get` أُعيد بعد التحليل والاختبار والبناء على القفل نفسه. لم يُطلب رفع إصدارات.

## 6. المخاطر المتبقية

- لا صور متصفح للدرجات 360…1920. اختبار الويدجت يثبت غياب استثناء التخطيط وبقاء الزر داخل الإطار، ولا يثبت مطابقة بكسل.
- صف جدول السجل الممتلئ عند عرض ≥900 لم يُضخ بمعرّف طويل في الاختبار.
- رجوع المتصفح أثناء نموذج متسخ يعتمد `PopScope`؛ الاختبار أثبت زر «رجوع» داخل التطبيق.
- الزائر بلا جلسة لا يرى 404 لأن الحارس يرسله إلى `/login`.
- التكامل الحي CSRF/CORS وACK وFCM يبقى **NOT VERIFIED / BLOCKED**.

## 7. الحكم

**CLOSED** لنطاق البرمجيات في `WEB-C-05`. التالية: `WEB-C-06` Final Software Verification & Documentation. لم تُبدأ هنا.

## 🧠 مقتطف هندسي من المهمة

زر «تسجيل جهاز» في رأس اللوحة كان أخاً غير مرن داخل `Row` في `OwnerPageHeader`. على عرض 360 يأخذ الزر عرضه الطبيعي أولاً، فإن زاد على المتبقي يفيض الصف ويخرج الزر من الإطار. تحت 560 يُبنى `Column`: النص ثم الزر، فيبقى الإجراء داخل `LayoutBuilder` الذي يقيس عرض المحتوى لا عرض النافذة. يُستخدم هذا عندما يكون الإجراء زرّاً بعرض ذاتي والنص يلتف. التحقق: `admin dashboard fits 360.0 without overflow` يتأكد أن مستطيل الزر داخل 360×900، وأن الدرج يُفتح من زر القائمة.
