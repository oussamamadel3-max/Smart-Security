# التقرير النهائي — التسلسل الذاتي للويب (WEB-T-02…WEB-T-08)

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-18 |
| الوضع | **مكتمل ضمن نطاق العميل البرمجي** |
| الاستثناء | Autonomous Sequential Web Development — عُلّق انتظار `APPROVE TASK CLOSURE` لمهام هذا التسلسل |
| اختبارات ختامية | **80/80** · `flutter analyze` بلا مشاكل · `flutter build web --release` exit 0 (`Built build\web`، حوالي 395 ثانية) |
| الجاهزية | نطاق العميل مكتمل. **ليست** جاهزية إنتاج 100% |

`WEB-T-01` أُغلقت قبل التسلسل في 2026-11-17 بعد `APPROVE TASK CLOSURE`. هذا التقرير يغطي ما بُني بعده. أرقام الاختبار هنا لقطة إغلاق التسلسل (**80/80**) ولا تُستبدل بنتائج مرحلة الاستكمال.

## تحديث مرحلة الاستكمال (2026-11-18، لا يلغي الجدول أدناه)

`WEB-C-01`…`WEB-C-06` أُغلقت خارج عدّاد 8/8 و21/50. إعادة التحقق في `WEB-C-06`: analyze نظيف، **121/121**، `flutter build web --release` exit 0. الحكم **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING**. ليست جاهزية إنتاج. `WEB-EXT-09` مكتمل. `WEB-EXT-08` صار PARTIAL. التفاصيل: `docs/reports/web/completion/WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md`.

## المهام

| المهمة | الحكم | لقطة الاختبار عند الإغلاق |
|--------|--------|---------------------------|
| WEB-T-02 | CLOSED — جلسة/HTTP/CSRF header/single-flight. الحي CSRF/Origin/CORS وتعطيل الحساب NOT VERIFIED | 20/20 |
| WEB-T-03 | CLOSED — قائمة أجهزة المالك وPENDING مقابل ACTIVE. API حي NOT VERIFIED | 25/25 |
| WEB-T-04 | CLOSED (عميل) — تفاصيل وأوامر. G1/ACK وOWNERSHIP_SYNC الحي BLOCKED | 38/38 |
| WEB-T-05 | CLOSED (عميل) — سجل حوادث/أوامر. نقل ملكية حي NOT VERIFIED | 58/58 |
| WEB-T-06 | CLOSED (عميل) — حسابات وأجهزة أدمن. متصفح حي NOT VERIFIED | 66/66 قبل إضافة T-07 |
| WEB-T-07 | CLOSED (عميل) — سجل إدارة وتنبيه إداري. FCM الحي BLOCKED / NOT VERIFIED | 76/76 |
| WEB-T-08 | CLOSED (مجموعة قبول العميل) — دخان المسارات ودلالات الأمر وقائمة SRS | **80/80** |

اللقطات تراكمية: 80 هو حجم المجموعة النهائية، وليس مجموع الصفوف.

التقارير: `docs/reports/web/WEB-T-02-report.md` … `WEB-T-08-report.md`، والقائمة `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`.

## البنية

تطبيق Flutter Web في `admin_web` يستهلك `packages/smart_security_app_core`. الاتجاه عربي RTL. التوجيه عبر `GoRouter` و`authGuardRedirect` في `lib/core/router/`. الحالة بـ Riverpod.

الميزات: `features/auth` للدخول، `features/devices` لقائمة المالك والتفاصيل والأوامر والسجل، `features/admin` للحسابات والأجهزة والربط والنقل وسجل الإدارة والتنبيه. العرض يتحدث إلى ViewModel ثم UseCase. المستودعات تعيد `Result`. لا MQTT من المتصفح، ولا Firebase داخل مشروع الويب، ولا اعتماد بناء على `security_mobile`.

الأوامر: قبول HTTP بحالة `PENDING` لا يقلب المؤشر المؤكد. `EXECUTED` وحده تنفيذ مؤكد في النموذج. `REJECTED` حالة نهائية بلا تنفيذ مؤكد. `TIMED_OUT` غير محسوم ويبقي منع الأمر المتعارض حتى `reconciledAt`. شاشة جهاز الأدمن لا تعرض أزرار تشغيل المالك.

## المصادقة

- Access JWT في `AccessTokenHolder` داخل الذاكرة. لا `localStorage` / `sessionStorage` للرموز.
- قناة التجديد للويب Cookie (`HttpOnly` يديرها المتصفح). الاستعادة بلا CSRF مقروء تبقى غير مصادَق عليها في الاختبار.
- `CsrfHeaderInterceptor` يرفق `X-CSRF-TOKEN` عندما يُطلب ذلك للمسار الذي يحتاج الترويسة.
- `AuthInterceptor` يجدّد مرة واحدة لعدة 401 (single-flight). 403 لا يجدّد الجلسة ولا يُنهيها.
- الخروج يمسح Access ويزيد جيل الجلسة حتى لا يعيد تحديث متأخر الجلسة، والمستمعون يفرغون ذاكرة الأجهزة والسجل.
- `OWNER` يذهب إلى `/owner` و`ADMIN` إلى `/admin`. كل منهما يُمنع من مسار الآخر. الحارس ليس بديلاً عن 403 الخادم.

ما لم يُثبت حياً: Cookie بعد Reload، CSRF/Origin/CORS ضد Backend، وتعطيل الحساب من الخادم. التفاصيل في `admin_web/WEB-REMAINING-EXTERNAL-WORK.md`.

## مصفوفة SRS (مختصر)

التفصيل والأسماء في `docs/reports/web/WEB-ACCEPTANCE-CHECKLIST.md`.

| المجموعة | الحكم |
|----------|--------|
| القسم 10.1: دخول، قائمة، تفاصيل، سجل، لوحة أدمن | PASSED على مستوى العميل |
| FR-014 دلالات الأمر الأربع | PASSED (دلالات). ACK الحي BLOCKED |
| FR-015 / FR-017 على جهاز أو خادم حي | PARTIAL أو NOT VERIFIED |
| FR-026 حفظ التنبيه وعرض المحاولة | PASSED (عميل). تسليم FCM BLOCKED |
| FR-032 / FR-033 / FR-035 | PASSED (عميل) |
| NFR-002 / NFR-003 end-to-end | PARTIAL |
| WEB-07 نقل ملكية حي | NOT VERIFIED |
| G0 / G1 / G2 | NOT VERIFIED — لم تُغلق |

## الاختبار والبناء

| الأمر | النتيجة الختامية |
|--------|------------------|
| `flutter analyze` | exit 0 — No issues found |
| `flutter test` | exit 0 — **80/80** |
| `flutter build web --release` | exit 0 — `Built build\web` (حوالي 395 ثانية) |

ملف الدخان الجديد: `admin_web/test/features/acceptance/web_acceptance_smoke_test.dart` (4 اختبارات).

## العمل الخارجي المتبقي

| المعرف | الموضوع |
|--------|---------|
| WEB-EXT-01 | CSRF / Origin / CORS حي |
| WEB-EXT-02 | Reload مع Cookie حقيقي |
| WEB-EXT-03 | تعطيل حساب حي |
| WEB-EXT-04 | G1 / ACK / OWNERSHIP_SYNC حي |
| WEB-EXT-05 | نقل ملكية حي يُفرّغ السجل |
| WEB-EXT-06 | متصفح تفاعلي ضد Backend حقيقي |
| WEB-EXT-07 | تسليم FCM حي |
| WEB-EXT-08 | مقارنة بصرية مع `design/index.html` — عند هذا التقرير كانت NOT VERIFIED. في WEB-C-06 (2026-11-18) أصبحت PARTIAL |
| WEB-EXT-09 | تقديم `build/web` — عند هذا التقرير كانت NOT VERIFIED. اكتملت في WEB-C-06 بتاريخ 2026-11-18 |

## الجاهزية

بطاقات الويب **8/8**. مجموع النظام بعد هذه المزامنة **21/50** (باك 5/13، موبايل 8/8، ويب 8/8، والبقية لم تُغلق).

نطاق عميل الويب مكتمل: الشاشات والحراس ودلالات الأمر والجلسة القابلة للاختبار بلا خادم. الإنتاج غير جاهز ما دامت البنود الحية وG1/G2 مفتوحة. لا يُحتسب HTTP ولا اختبار المحاكاة تنفيذاً على الجهاز.

## حوكمة

- الاستثناء موثّق في `AGENTS.md` وفي `.cursor/rules/common/task-execution.mdc` (استثناء الويب).
- لم يُعتمد G0/G1/G2 ولم تُغيّر عقود Backend أو ESP أو integration.
- لا مهمة ويب تالية تُبدأ تلقائياً من `web-current.md`.
