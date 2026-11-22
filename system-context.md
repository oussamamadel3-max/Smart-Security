# السياق العام — نظام الأمان الذكي

آخر تحديث: 2026-11-21 (تثبيت قرارات مالك G0/G1؛ البوابات غير مغلقة؛ 21/50 بلا تغيير)  
الغرض: صورة واحدة لحالة كل المشاريع وتبعياتها؛ التفاصيل داخل `*-context.md` لكل مجلد.  
التقدّم الزمني والمؤشرات: [`system-progress.md`](./system-progress.md) · **توضيح التسميات** في أعلى ذلك الملف.  
قرارات المالك: [`docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md`](./docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md) · الاعتماديات: [`docs/gates/G0-G1-remaining-dependencies.md`](./docs/gates/G0-G1-remaining-dependencies.md)

## صورة النظام

- **سلامة محلية:** حساس ← ESP32 ← صفارة (دون إنترنت).
- **تحكم:** واجهة ← HTTPS ← خادم ← MQTT ← جهاز ← تأكيد ← DB/واجهة.
- **إنذار بعيد:** جهاز ← MQTT ← حفظ حادثة ← طلب إشعار ← FCM ← هاتف.
- **مصدر الحقيقة:** آخر حالة أكدها الجهاز — لا نجاح HTTP ولا وصول وسيط وحده.
- Flutter لا يتصل بـ MQTT مباشرة.
- **2026-11-16:** وُافق على قرارات عقد G1 الثمانية في `docs/gates/G1-contract-decisions.md` ونُفذت على المسودة والخادم. البوابة تبقى `G1 BLOCKED`. لم تتغير عدّادات المهام أعلاه.

## المراجع العليا

| وثيقة | مسار |
|--------|------|
| SRS v1.0 | [`docs/SRS_نظام_الأمان_الذكي_v1.0.md`](./docs/SRS_نظام_الأمان_الذكي_v1.0.md) · `.docx` في الجذر |
| خطة التطوير | [`docs/خطة_تطوير_نظام_الأمان_الذكي_v1.0.md`](./docs/خطة_تطوير_نظام_الأمان_الذكي_v1.0.md) |
| تصميم DB | [`docs/تصميم_قاعدة_بيانات_نظام_الأمان_الذكي_v1.0.md`](./docs/تصميم_قاعدة_بيانات_نظام_الأمان_الذكي_v1.0.md) |
| مواصفات التشغيل | [`docs/مواصفات_نظام_الأمان_الذكي.md`](./docs/مواصفات_نظام_الأمان_الذكي.md) |
| تعريف الإنجاز | [`integration/integration-definition-of-done.md`](./integration/integration-definition-of-done.md) |

## خريطة المشاريع

| مجلد | الدور | سياق | تقدّم | مهمة جارية | عدّ |
|-------|--------|-------|--------|------------|-----|
| `security_backEnd` | API · Identity · MQTT عمال · FCM خادم · PostgreSQL | [`backend-context`](./security_backEnd/backend-context.md) | [`backend-progress`](./security_backEnd/backend-progress.md) | T-05…T-08/T-10 تنفيذ اختباري بلا `[x]`؛ الإغلاق الإنتاجي بعد G1 حيث يلزم؛ MQTT/FCM إنتاجي معطّل | 5/13 |
| `security_esp` | عتاد + firmware + إنذار محلي | [`esp-context`](./security_esp/esp-context.md) | [`esp-progress`](./security_esp/esp-progress.md) | قرارات مالك مثبّتة؛ **G0 NOT CLOSED**؛ ESP-T-01 بلا إغلاق؛ ESP-C 6/6؛ لا flash | 0/9 |
| `security_mobile` | تطبيق مالك Flutter + FCM عميل + Wi-Fi محلي · يستهلك `packages/smart_security_app_core` | [`mobile-context`](./security_mobile/mobile-context.md) | [`mobile-progress`](./security_mobile/mobile-progress.md) | تسلسل T-01…T-08 مكتمل (عميل؛ ميداني مؤجّل) | 8/8 |
| `admin_web` | Flutter Web مالك + أدمن · يستهلك نفس الحزمة المشتركة | [`web-context`](./admin_web/web-context.md) | [`web-progress`](./admin_web/web-progress.md) | لا تالية. T-01…T-08 وWEB-C مغلقة (عميل). الحي معلّق. ليست جاهزية إنتاج | 8/8 |
| `integration` | عقود MQTT/HTTP · G1/G2 · مصفوفة قبول | [`integration-context`](./integration/integration-context.md) | [`integration-progress`](./integration/integration-progress.md) | **INT-T-01** التالية المعتمدة بعد تثبيت القرارات — لم تُبدأ في جولة التوثيق | 0/6 |
| `ops` | Compose · بيئات · نشر · نسخ · ميداني | [`ops-context`](./ops/ops-context.md) | [`ops-progress`](./ops/ops-progress.md) | **OPS-T-01** بعد INT-T-01 في التسلسل المعتمد — لم تُبدأ | 0/6 |

## ترتيب التنفيذ المقترح (مرتفع المستوى)

| مرحلة | المعنى | أين تُحرَّك الآن |
|--------|--------|-------------------|
| **P0** | قرارات عتاد/عقود/بيئات | قرارات مالك G0/G1 **مُسجَّلة** 2026-11-21؛ التالي المعتمد: **INT-T-01** ثم **OPS-T-01** (بلا بدء في جولة التوثيق) |
| **P1** | تجربة عتاد محلية | بعد توفر القطع ورفع تأجيل D-01 → `security_esp` T-02…؛ G0 تبقى مفتوحة حتى الموافقة والأدلة |
| **P2** | DB + MQTT محلي | باك T-01…T-04 ✓ وT-09 ✓ · تقوية مسودة T-05…T-10 بلا تفعيل إنتاج · Compose عبر OPS-T-01 |
| **P3** | شريحة متكاملة أولى | بعد أدلة G1: أوامر/حوادث + ESP MQTT + واجهات |
| **P4…P7** | بقية SRS → واجهات → VPS → قبول G2 | حسب المهام في كل `*-context` |

**استقلالية المصادقة:** إنجاز Backend T-02 يسمح لعميل Web/Mobile T-02 بالعمل على عقد HTTP الجاري التحقق منه، دون انتظار MQTT/G1.

## تبعيات حرجة بين المشاريع

```
security_esp T-01 (G0 / D-01…D-07)
        │
        ├─► integration T-01 (عقد MQTT) ─► T-02 (D-09) ─► T-04 (G1)
        │         │                              │
        │         └──────────────┬───────────────┘
        │                        ▼
ops T-01 (Compose)      security_backEnd T-03+ (أجهزة/MQTT/أوامر بعد الاعتماد الملائم)
        │                        │
        │                        ├─► security_mobile T-03+ / admin_web T-03+ (أجهزة/أوامر)
        │                        └─► security_esp T-06+ (MQTT إنتاجي)
        ▼
ops T-02…T-06 (نشر/نسخ/ميداني)  +  integration T-05 (G2)
```

- **لا أوامر/حوادث إنتاج** قبل اعتماد **G1**.
- **لا إغلاق قبول نهائي** قبل **G2** ومصفوفة التكامل.
- مصادقة Backend T-02 منفذة بحسب تقرير Cursor (JWT + Refresh Web Cookie/CSRF وMobile Body)، ويمكن البدء بتصميم عميل Flutter بعد التحقق من OpenAPI الفعلي؛ **G1 لا يمنع تأسيس العميل أو تسجيل الدخول**، لكنه يمنع إغلاق مسارات الأوامر والحوادث الإنتاجية.

## قرارات معلّقة على مستوى النظام

| رمز | الموضوع | المالك الأساسي | الحالة |
|-----|---------|----------------|--------|
| D-01 | قطع إلكترونية نهائية + متطلبات منتج (SW-420 / 12V / إنذار محلي) | العتاد | `DEFERRED — HARDWARE DECISION PENDING` (متطلبات المنتج السابقة نافذة؛ BOM غير معتمد للشراء) |
| D-02 | قاعدة تحقق حادثة الاهتزاز | العتاد + منتج | `OWNER APPROVED — HARDWARE CALIBRATION PENDING` (أرقام `TEST_ONLY`) |
| D-03 | 30ث / 60د / ACK 10ث / تغطية ~ساعة | العتاد + Backend | `PROVISIONAL DEVELOPMENT VALUES — PRODUCTION VALIDATION PENDING` (فصل ACK Timeout عن Command TTL) |
| D-04 | Wi-Fi محلي + هوية MQTT + تدوير اعتماد | ESP + تشغيل + أمن | `OWNER APPROVED — MANUAL ROTATION FOR V1` |
| S1 | سلامة تلف الإعداد / فقد الطاقة الكامل | منتج + ESP | `OWNER APPROVED — S1 — VALIDATION PENDING` |
| D-05 | وقت موثوق وتسلسل بعد إعادة التشغيل | ESP + Backend | مبدأ معتمد سابقاً؛ إثبات العتاد معلّق |
| D-06 | أرشفة واحتفاظ | المنتج + التشغيل | مبادئ معتمدة؛ التحقق التشغيلي معلّق |
| D-07 | ASP.NET Core Identity / UUID | Backend + DB | معتمد معمارياً؛ بلا إعادة فتح |
| D-08 | OTA | عتاد/منتج | مؤجّل — بلا إعادة فتح |
| D-09 | COMMAND_ACK كامل + resultStateSequence | `integration` + ESP + باك | وظيفي معتمد؛ اعتماد G1 التفصيلي معلّق |
| MQTT/TLS | إنتاج مشفّر فقط؛ محلي معزول مسموح | تشغيل + أمن | `OWNER APPROVED — ISOLATED LOCAL INTEGRATION ALLOWED — PRODUCTION DISABLED` |
| G1 عقد | القرارات الثمانية 2026-11-16 | `integration` | نافذة؛ **ليست** اجتياز بوابة |
| G1 بوابة | اعتماد عقد الرسائل/HTTP بأدلة | `integration` | `BLOCKED` |
| G2 | قبول AC-T-01…AC-T-25 | `integration` | `NOT CLOSED` |


**وضع G0:** `NOT CLOSED`. تثبيت قرارات المالك ≠ إغلاق البوابة. التفاصيل: `docs/gates/G0-decision.md`.

## لقطة حالة سريعة

- Backend: T-01…T-04 و**T-09** مكتملة رسمياً (2026-11-06 أُغلق `BE-T-09` خلف Fake FCM؛ Firebase الحي معطّل). T-06…T-08 تنفيذ اختباري بلا إغلاق رسمي. التالية المؤهلة **T-05** أوامر/Outbox (لم تُبدأ؛ إغلاق إنتاجي بعد G1). **استقبال MQTT الحي واعتماد G1 غير منجزين**؛ AC-T-19/Broker معلقان.
- Flutter: **MOB-T-01…T-08** مكتملة ضمن نطاق العميل (Autonomous Sequential؛ test 34/34). بطاقات G1/Firebase/soft-AP/جهاز مؤجّلة. **WEB-T-01…T-08** مغلقة ضمن نطاق العميل (لقطة T-08: test 80/80). استكمال **WEB-C-01…C-06** مغلق 2026-11-18 خارج العدّاد (إعادة تحقق C-06: test 121/121) والحكم **SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING**. G1/ACK الحي **BLOCKED**. نقل الملكية الحي **NOT VERIFIED**. FCM الحي **NOT VERIFIED / BLOCKED**. CSRF/Origin/CORS الحي وتعطيل الحساب والمتصفح ضد Backend **NOT VERIFIED / BLOCKED**. `WEB-EXT-09` مكتمل محليًا. `WEB-EXT-08` PARTIAL. لا مهمة ويب تالية. إغلاق البطاقات ليس جاهزية إنتاج.
- التالي المعتمد (بعد جولة التوثيق): **INT-T-01** ثم **OPS-T-01** ثم INT-T-02/T-03 ثم تقوية Backend المستقلة؛ العتاد/الخارجي لاحقاً. ويب وموبايل: عميل مكتمل برمجياً. **ليس PRODUCTION READY.**

## كيف تُحدَّث ملفات السياق والتقدّم

1. نفّذ عبر `*-current.md` → عند معلم ملموس حدّث `*-progress.md` للمشروع (سجل + مؤشرات).
2. إن تغيّر النطاق/المانع/المهمة الجارية: حدّث `*-context.md` للمشروع ثم صف الخريطة هنا.
3. انقل عدّ اللوحة إلى [`system-progress.md`](./system-progress.md).
4. علّم `[x]` في `*-development-tasks.md` فقط بعد القبول وتعريف الإنجاز في `integration`.
5. لا تضع أسراراً في `*-context` أو `*-progress`.
