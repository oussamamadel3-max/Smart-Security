# التقدّم العام — نظام الأمان الذكي

آخر تحديث: 2026-11-06  
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
| باك إند | [`security_backEnd/backend-progress.md`](./security_backEnd/backend-progress.md) | 3 / 13 | التالية T-04 (لم تُبدأ) | ~23% |
| ESP | [`security_esp/esp-progress.md`](./security_esp/esp-progress.md) | 0 / 9 | T-01 | 0% |
| موبايل | [`security_mobile/mobile-progress.md`](./security_mobile/mobile-progress.md) | 0 / 8 | T-01 | 0% |
| ويب | [`admin_web/web-progress.md`](./admin_web/web-progress.md) | 0 / 8 | T-01 | 0% |
| تكامل | [`integration/integration-progress.md`](./integration/integration-progress.md) | 0 / 6 | T-01 | 0% |
| تشغيل | [`ops/ops-progress.md`](./ops/ops-progress.md) | 0 / 6 | T-01 | 0% |
| **المجموع** | — | **3 / 50** | — | **6%** |

## سجل عام (معالم فقط)

| التاريخ | المشروع | الحدث |
|---------|---------|--------|
| 2026-11-04 | الكل | إنشاء `*-context.md` و`*-progress.md` لكل مشروع + `system-*`؛ نسخ مراجع `docs/*.md` |
| (سابق) | `security_backEnd` | إكمال **T-01** (مخطط DB + هيكل الحل) |
| 2026-11-04 | `security_backEnd` | إقرار إغلاق T-02 توثيقياً وفق تقرير Cursor بنتائج 60/60 اختباراً وبناء نظيف؛ T-03 التالية ولم يبدأ تنفيذها. |
| 2026-11-05 | `security_backEnd` | إغلاق بطاقة **BE-02/SS-P2-010** (تسجيل جهاز ذرّي)؛ T-03 تبقى جزئية؛ التالية SS-P2-020. |
| 2026-11-06 | `security_backEnd` | إغلاق بطاقة **BE-02/SS-P2-020** (ربط PENDING / إقرار الملكية)؛ T-03 تبقى جزئية (2/3)؛ التالية SS-P2-030. |
| 2026-11-06 | `security_backEnd` | إغلاق **BE-02/SS-P2-030** وإغلاق **T-03** كاملة؛ التالية T-04 (لم تُبدأ). |

## التالي على مستوى النظام

1. العقود والعتاد أولاً حيث يلزم؛ Backend عند **T-04**؛ ESP **T-01 (G0)** وintegration **T-01** وops **T-01**  
2. تأسيس Flutter: mobile/web **T-01**  
3. لا أوامر/حوادث إنتاج قبل **G1**

## قواعد المزامنة

1. حدّث `*-progress.md` الخاص بالمشروع أولاً (سجل + مؤشرات).
2. انقل صف اللوحة هنا إن تغيّر العدّ أو المهمة الجارية.
3. حدّث `*-context.md` إن تغيّر المانع أو الهوية أو التبعية — لا تكرر السجل الزمني هناك.
4. علّم `[x]` في `*-development-tasks.md` فقط بعد القبول وتعريف الإنجاز.

**حدود الدليل:** عدّ المهام المكتملة على مستوى T-xx = 3/13 للباك بعد إغلاق T-03؛ T-04 لم تُبدأ.
