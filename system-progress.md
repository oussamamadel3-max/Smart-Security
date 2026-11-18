# التقدّم العام — نظام الأمان الذكي

آخر تحديث: 2026-11-18  
السياق العام: [`system-context.md`](./system-context.md)

## توضيح التسميات (ملفات التخطيط)

| التسمية | أين | ماذا تحفظ | متى تُحدَّث |
|---------|-----|-----------|-------------|
| **`*-context.md`** | جذر (`system-`) + كل مشروع ببادئته | هوية، مبادئ، موانع، لقطة حالة، تبعيات | عند تغيّر النطاق أو القرار أو المهمة الجارية |
| **`*-progress.md`** | جذر (`system-`) + كل مشروع ببادئته | مؤشرات عدّ، سجل زمني، عوائق التقدّم، نقطة تحقق | عند بدء/إيقاف/إكمال عمل أو معلَم ملموس |
| **`*-current.md`** | داخل المشروع فقط | ورقة المهمة الجارية وحدها (مطلوب/قبول/تم/تحقق) | عند اختيار مهمة للتنفيذ وطوال العمل عليها |
| **`*-development-tasks.md`** | داخل المشروع فقط | قائمة كل المهام T-xx وبطاقاتها `[ ]`/`[x]` | عند إغلاق مهمة بعد القبول |
| **`*-development-plan.md`** | داخل المشروع فقط | نطاق المراحل وما يُبنى / لا يُعاد | نادراً — عند تغيّر الخطة |
| **`docs/`** | جذر (مراجع) + باك (`security_backEnd/docs`) | SRS/خطة/DB منسوخة للرجوع؛ وثائق تقنية للتنفيذ | عند اعتماد وثيقة أو ترحيل |

**قاعدة سريعة:**  
`*-context` = *أين نحن ولماذا* · `*-progress` = *كم أنجزنا ومتى* · `*-current` = *ماذا نعمل الآن بالتفصيل* · `*-development-tasks` = *قائمة الإغلاق الرسمية*.

أسماء الملفات حسب المشروع:

| مجلد | البادئة | السياق | التقدّم |
|-------|---------|--------|---------|
| _(الجذر)_ | `system-` | `system-context.md` | `system-progress.md` |
| `security_backEnd` | `backend-` | `backend-context.md` | `backend-progress.md` |
| `security_esp` | `esp-` | `esp-context.md` | `esp-progress.md` |
| `security_mobile` | `mobile-` | `mobile-context.md` | `mobile-progress.md` |
| `admin_web` | `web-` | `web-context.md` | `web-progress.md` |
| `integration` | `integration-` | `integration-context.md` | `integration-progress.md` |
| `ops` | `ops-` | `ops-context.md` | `ops-progress.md` |

## لوحة التقدّم

| مشروع | ملف التقدّم | مكتمل / الكل | الجارية | النسبة |
|--------|-------------|--------------|---------|--------|
| باك إند | [`security_backEnd/backend-progress.md`](./security_backEnd/backend-progress.md) | 5 / 13 | التالية T-05 (لم تُبدأ؛ T-09 ✓) | ~38% |
| ESP | [`security_esp/esp-progress.md`](./security_esp/esp-progress.md) | 0 / 9 | T-01 | 0% |
| موبايل | [`security_mobile/mobile-progress.md`](./security_mobile/mobile-progress.md) | 8 / 8 | تسلسل عميل مكتمل | 100% |
| ويب | [`admin_web/web-progress.md`](./admin_web/web-progress.md) | 8 / 8 | لا تالية. استكمال WEB-C مغلق خارج العدّاد. الحي وEXT-08 معلّقان | 8/8 بطاقات؛ ليست جاهزية إنتاج |
| تكامل | [`integration/integration-progress.md`](./integration/integration-progress.md) | 0 / 6 | T-01 | 0% |
| تشغيل | [`ops/ops-progress.md`](./ops/ops-progress.md) | 0 / 6 | T-01 | 0% |
| **المجموع** | — | **21 / 50** | — | **42%** |

## سجل عام (معالم فقط)

| التاريخ | المشروع | الحدث |
|---------|---------|--------|
| 2026-11-04 | الكل | إنشاء `*-context.md` و`*-progress.md` لكل مشروع + `system-*`؛ نسخ مراجع `docs/*.md` |
| (سابق) | `security_backEnd` | إكمال **T-01** (مخطط DB + هيكل الحل) |
| 2026-11-04 | `security_backEnd` | إقرار إغلاق T-02 توثيقياً وفق تقرير Cursor بنتائج 60/60 اختباراً وبناء نظيف؛ T-03 التالية ولم يبدأ تنفيذها. |
| 2026-11-05 | `security_backEnd` | إغلاق بطاقة **BE-02/SS-P2-010** (تسجيل جهاز ذرّي)؛ T-03 تبقى جزئية؛ التالية SS-P2-020. |
| 2026-11-06 | `security_backEnd` | إغلاق بطاقة **BE-02/SS-P2-020** (ربط PENDING / إقرار الملكية)؛ T-03 تبقى جزئية (2/3)؛ التالية SS-P2-030. |
| 2026-11-06 | `security_backEnd` | إغلاق **BE-02/SS-P2-030** وإغلاق **T-03** كاملة؛ التالية T-04 (لم تُبدأ). |
| 2026-11-06 | `security_backEnd` | إغلاق **T-04 / BE-T-04** (وارد حالة + API مؤشرات)؛ Mosquitto الحي وG1 **غير منجزين**؛ التالية T-05 (لم تُبدأ). |
| 2026-11-06 | `security_backEnd` | إغلاق **T-09 / BE-T-09** (FCM Fake + push_tokens)؛ Firebase الحي معطّل؛ T-06…T-08 بلا إغلاق رسمي؛ لا بدء تلقائي لـ T-10. |
| 2026-11-17 | Flutter | إغلاق **MOB-T-01** و**WEB-T-01**. تشغيل Android/iOS مؤجّل بموافقة المالك وليس نجاحاً. التالية الموبايل T-02 بانتظار التنفيذ؛ WEB-T-02 لم تبدأ. |
| 2026-11-17 | `security_mobile` | إغلاق **MOB-T-02** بعد `APPROVE TASK CLOSURE` (جلسة/HTTP/single-flight؛ test 14/14؛ APK dev). |
| 2026-11-17 | حوكمة | استثناء Autonomous Sequential Mobile في `AGENTS.md` و`task-execution.mdc` (إغلاق/سلسلة تلقائية لـ MOB فقط). |
| 2026-11-17 | `security_mobile` | إغلاق تلقائي **MOB-T-03** (قائمة أجهزة؛ test 20/20؛ APK). بدء **T-04**. |
| 2026-11-17 | `security_mobile` | **MOB-T-04** عميل أوامر جاهز (test 22/22؛ APK)؛ التسلسل توقف — بطاقات ACK تحتاج G1. |
| 2026-11-17 | حوكمة | تعديل AGENTS/6A: تعليق `APPROVE TASK CLOSURE` للموبايل؛ G1 المؤجّل لا يوقف السلسلة. |
| 2026-11-17 | `security_mobile` | إغلاق تلقائي **T-04…T-08** (نطاق عميل). test ختامي **34/34**. تقرير: `docs/reports/mobile/MOBILE-FINAL-REPORT.md`. |
| 2026-11-17 | `admin_web` | إغلاق تلقائي **WEB-T-04** (تفاصيل وأوامر؛ test 38/38؛ `flutter build web --release`). G1/ACK وOWNERSHIP_SYNC الحي **BLOCKED**. التالية **WEB-T-05**. |
| 2026-11-17 | `admin_web` | إغلاق تلقائي **WEB-T-05** (سجل حوادث/أوامر؛ test 58/58؛ `flutter build web --release`). نقل ملكية حي **NOT VERIFIED**. التالية **WEB-T-06**. |
| 2026-11-18 | `admin_web` | إغلاق تلقائي **WEB-T-07** (سجل إدارة وتنبيه إداري؛ test 76/76؛ `flutter build web --release`). تسليم FCM الحي **NOT VERIFIED / BLOCKED**. زُامن عدّ T-06 الذي كان مغلقاً في قائمة المهام. التالية **WEB-T-08**. |
| 2026-11-18 | `admin_web` | إغلاق تلقائي **WEB-T-08** (مجموعة قبول العميل؛ test 80/80؛ `flutter analyze` نظيف؛ `flutter build web --release`). CSRF/Origin الحي وG1/ACK ونقل الملكية الحي وFCM الحي والمتصفح ضد Backend حقيقي **BLOCKED / NOT VERIFIED**. لا مهمة ويب تالية داخل التسلسل. التقرير: `docs/reports/web/WEB-FINAL-REPORT.md`. |
| 2026-11-18 | `admin_web` | إغلاق استكمال **WEB-C-01…C-06** خارج 21/50 (إعادة تحقق C-06: test 121/121). الحكم SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING. `WEB-EXT-09` مكتمل محليًا. `WEB-EXT-08` PARTIAL. ليست جاهزية إنتاج. التقرير: `docs/reports/web/completion/WEB-SOFTWARE-COMPLETION-FINAL-REPORT.md`. |

## التالي على مستوى النظام

1. العقود والعتاد أولاً حيث يلزم؛ Backend التالية المؤهلة **T-05** (إغلاقها الإنتاجي بعد G1)؛ ESP **T-01 (G0)** وintegration **T-01** وops **T-01**  
2. Flutter موبايل والويب: تسلسل العميل مكتمل (MOB-T-01…T-08 وWEB-T-01…T-08)، واستكمال الويب WEB-C-01…C-06 مغلق خارج العدّاد. الميداني وG1 وCSRF/CORS الحي وACK الحي ونقل الملكية الحي وFCM الحي والمتصفح ضد Backend حقيقي والمطابقة البصرية المتبقية مؤجّلة وموثّقة، وليست جاهزية إنتاج.  
3. لا أوامر/حوادث إنتاج قبل **G1**؛ استقبال MQTT الحي عبر Mosquitto ما زال غير منجز؛ FCM إنتاجي غير مفعّل

## قواعد المزامنة

1. حدّث `*-progress.md` الخاص بالمشروع أولاً (سجل + مؤشرات).
2. انقل صف اللوحة هنا إن تغيّر العدّ أو المهمة الجارية.
3. حدّث `*-context.md` إن تغيّر المانع أو الهوية أو التبعية — لا تكرر السجل الزمني هناك.
4. علّم `[x]` في `*-development-tasks.md` فقط بعد القبول وتعريف الإنجاز.

**حدود الدليل:** عدّ المهام المكتملة على مستوى T-xx = 5/13 للباك بعد إغلاق T-09، و8/8 للموبايل (نطاق عميل)، و8/8 للويب (نطاق عميل؛ المجموع 21/50). T-05 لم تُبدأ رسمياً كمسار أوامر في الباك؛ T-06…T-08 في الباك تنفيذ اختباري بلا `[x]`؛ Mosquitto الحي وG1 وFCM الإنتاجي غير منجزين. تشغيل تطبيق الموبايل على جهاز/محاكٍ وiOS مؤجّلان وليسا ضمن دليل الإغلاق. تحقق ACK الحي لأوامر الويب **BLOCKED**. نقل ملكية حي لسجل ويب T-05 **NOT VERIFIED**. تسليم FCM الحي لتنبيه الويب T-07 **NOT VERIFIED / BLOCKED**. CSRF/Origin/CORS الحي وتعطيل الحساب الحي والمتصفح التفاعلي ضد Backend حقيقي **NOT VERIFIED / BLOCKED**. إغلاق بطاقات الويب 8/8 لا يعني جاهزية إنتاج 100%.
