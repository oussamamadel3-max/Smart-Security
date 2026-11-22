# الاعتماديات والأعمال المؤجلة — G0 / G1

**آخر تحديث:** 2026-11-22 (OPS-T-03 تحقق محلي تكميلي؛ HTTPS العام معلّق؛ G1 BLOCKED؛ 26/50)  
**الحالة:** مرجع مركزي لما لا يُكمل حالياً بسبب عتاد أو معايرة أو أدلة ميدانية أو اعتمادات خارجية.  
**لا يدخل في عدّاد المهام الرسمية 50.**  
**مرجع قرارات المالك لهذه الجولة:** [`../reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md`](../reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md)  
**مسودة INT-T-01:** [`../../integration/contracts/MQTT-V1-DRAFT.md`](../../integration/contracts/MQTT-V1-DRAFT.md)  
**حالة البوابات:** `G0 = NOT CLOSED` · `G1 = BLOCKED` · `G2 = NOT CLOSED`

## خريطة المراجع الأساسية (لتفادي تضارب النسخ)

| الموضوع | المرجع الأساسي |
|---------|----------------|
| قرارات مالك 2026-11-21 | `docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md` |
| سجل D-01…D-07 + سياسة السلامة S1 | `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md` |
| حالة بوابة G0 | `docs/gates/G0-decision.md` |
| حالة بوابة G1 | `docs/gates/G1-decision.md` |
| قرارات عقد G1 الثمانية | `docs/gates/G1-contract-decisions.md` |
| أدلة تشغيل G1 | `docs/gates/G1-evidence.md` |
| هذا الكتالوج | `docs/gates/G0-G1-remaining-dependencies.md` |

---

## الكتالوج

| المعرّف | وصف العمل | سبب التأجيل | مرتبط بـ | شرط الاستئناف | دليل القبول المطلوب | الحالة |
|---------|-----------|-------------|----------|----------------|----------------------|--------|
| DEP-HW-01 | اعتماد القطع الإلكترونية النهائية (لوحة/منظم/صفارة/بطارية/حماية) والشراء | تأجيل مالك لـ D-01؛ BOM أولي فقط | D-01 · ESP-T-01/T-02 · G0 | قرار مالك نهائي لأرقام القطع + توفر شراء | BOM موقّع + مخطط EDA/توصيل مراجَع | `DEFERRED — HARDWARE DECISION PENDING` |
| DEP-HW-02 | قياس تيار الصفارة/المنظم وسجل سلامة 12V | لا عتاد حقيقي | D-01 · ESP-T-02 · G0 | توفر دائرة طاولة آمنة | سجل سلامة كهربائية موقّع + قياسات تيار | `BLOCKED BY HARDWARE` |
| DEP-HW-03 | معايرة SW-420 (N / نافذة T / تبريد) | لا عتاد؛ القيم `TEST_ONLY` | D-02 · ESP-T-02/T-03 · G0 | عتاد مطابق + تنفيذ خطة المعايرة | نتائج معايرة رقمية موثّقة | `OWNER APPROVED RULE — CALIBRATION PENDING` |
| DEP-HW-04 | قياس TTL الإنتاجي وسعة الطابور وميزانية كتابة NVS | قيم تطوير مؤقتة فقط | D-03 / NFR-008 · G0/G1-02 | عتاد + سيناريوهات حمل | تقرير قياس سعة/TTL/NVS مفصول عن ACK Timeout | `PROVISIONAL — PRODUCTION VALIDATION PENDING` |
| DEP-HW-05 | إثبات استمرار `stateSequence` بعد قطع طاقة | لا اختبار قطع طاقة على Flash | D-05 · G1-CD-7 · G1-06 | عتاد ESP32 + بروتوكول قطع طاقة | سجل تسلسل قبل/بعد القطع في PostgreSQL بلا `IGNORED_STALE_SEQUENCE` خاطئ | `PRINCIPLE APPROVED — HARDWARE VALIDATION PENDING` |
| DEP-HW-06 | إثبات رفض الأمر البعيد عند تعذر `expiresAt` / `clockTrusted=false` | لا firmware حي مثبت | G1-CD-5 · FR-017 · ESP-T-07 | جهاز متصل بعقد المسودة | اختبار عتاد: رفض أمر بلا وقت موثوق مع استمرار الإنذار المحلي | `CONTRACT APPROVED — HARDWARE VALIDATION PENDING` |
| DEP-HW-07 | تخزين أحداث محلي على ESP عند انقطاع الشبكة | persistence Mosquitto لا يغني | D-06 (جهاز) · G1-07 · ESP-T-05 | عتاد + انقطاع شبكة مُتحكَّم | لا حذف قبل `EVENT_PERSISTED` المطابق للمرحلة؛ علامة فقد عند الامتلاء | `SOFTWARE COMPLETE PARTIAL — HARDWARE VALIDATION PENDING` |
| DEP-SAFE-01 | تحقق سياسة السلامة S1 (استعادة NVS الصحيحة / استرداد آمن عند التلف) | قرار معتمد؛ التحقق معلّق | S1 · ESP-T-01/T-05/T-09 · G0 | سيناريوهات تلف إعداد + فقد طاقة كامل ثم عودة | سلوك مطابق لـ S1 + إعلان عطل صريح بلا حماية مؤكدة وهمية | `OWNER APPROVED — S1 — VALIDATION PENDING` |
| DEP-OPS-01 | شهادة TLS / تفعيل MQTT إنتاجي (طبقات منفصلة) | الإنتاج معطّل بسياسة مالك؛ تجهيز الشهادة **لا** يُحظر بانتظار إغلاق G1 | G1-CD-8 · G1-03 · OPS · `G1-decision.md` طبقات TLS | (أ) تجهيز شهادة/مسار سري معزول يمكن أن يبدأ دون إغلاق G1 · (ب/ج) أدلة تحقق لمعايير G1 · (د) تفعيل إنتاج فقط بعد أدلة + تفويض صريح | دليل كل طبقة على حدة؛ لا نسخ شهادات اختبار للإنتاج؛ لا `UseTls` إنتاجي بلا الطبقة (د) | `OWNER APPROVED POLICY — PRODUCTION DISABLED` (التجهيز مسموح؛ التفعيل لا) |
| DEP-OPS-02 | Compose محلي رسمي (PostgreSQL + Mosquitto + ACL) | **أُغلق OPS-T-01** 2026-11-22 | OPS-T-01 ✓ · يسرّع أدلة MQTT المحلية | — | مكدس يقلع + ACL 9/9 + TLS طبقة أ/ب؛ التقارير `OPS-T-01-report.md` / `OPS-T-01-CLOSURE-REPORT.md` | `CLOSED — LOCAL STACK ONLY (NOT G1 / NOT PRODUCTION)` |
| DEP-INT-01 | ملكية Integration الرسمية لمسودة MQTT/آلات الحالات | **أُغلقت INT-T-01** 2026-11-22 — مسودة v1 مقبولة ضمن النطاق؛ ليست اعتماد إنتاج/G1 | INT-T-01 ✓ · مدخل G1 | اعتماد G1 عبر INT-T-04 بعد الأدلة | محضر + إغلاق رسمي + عدم خلط DRAFT بإنتاج | `CLOSED AS DRAFT — G1 STILL BLOCKED` |
| DEP-FLUT-01 | عرض `healthCode` / S1 UX (لا محمي مؤكد عند FAULT/UNKNOWN) | فجوة مراجعة INT-T-01 Flutter | Mobile R-12 · WEB-EXT-10 · DEP-SAFE-01 | مهمة عميل لاحقة | اختبار عرض مركّب | `OPEN — DOCUMENTED` |
| DEP-FLUT-02 | حراس أوامر Mobile parity مع Web | فجوة مراجعة INT-T-01 | Mobile R-13 · WEB-EXT-11 | اختبارات Mobile | suite مكافئ لـ `device_detail_command_guards_test` | `OPEN — DOCUMENTED` |
| DEP-INT-02 | أمثلة D-09 وترتيب ACK/STATE + OpenAPI المشترك + محضر ثلاثي | **INT-T-02/T-03 أُغلقتا** (توثيق D-09 + مسودة HTTP)؛ T-04 مفتوحة | INT-T-02 ✓ · INT-T-03 ✓ · INT-T-04 · G1 | `START TASK: INT-T-04` + أدلة G1 | محضر ESP+Backend+Flutter + أدلة G1-01…G1-10 | `INT-T-03 CLOSED AS HTTP DRAFT — T-04 OPEN — G1 STILL BLOCKED` |
| DEP-FCM-01 | Firebase/FCM حي (توكن جهاز، تسليم، Console) | اعتماد خارجي؛ الإنتاج Fake افتراضياً | BE-T-09 · FCM-C · Mobile | جهاز/مشروع Firebase مفوّض + شبكة مستقرة | توكن حي + استقبال fg/bg/terminated موثّق | `SOFTWARE COMPLETE — EXTERNAL DEPENDENCIES PENDING` |
| DEP-MOB-01 | تشغيل الموبايل على جهاز/محاكٍ حقيقي وقبول ميداني | مؤجّل بموافقة مالك سابقة | MOB remaining · G2 جزئياً | جهاز Android/iOS + Backend متاح | سجل تشغيل يدوي للمسارات الحرجة | `DEFERRED — DEVICE RUN PENDING` |
| DEP-WEB-01 | CSRF/Origin/CORS حي وتعطيل حساب ومتصفح ضد Backend حقيقي | قوالب + قائمة H1–H7 جاهزة؛ HTTPS المختبر المحلي (`local_certs`) ليس بديلاً | WEB-EXT · OPS-T-03 · G1 جزئياً | مضيف HTTPS staging مفوّض | محضر H1–H7 بلا أسرار | `CHECKLIST READY — LIVE HTTPS PENDING` |
| DEP-OPS-03 | مضيف Staging حقيقي (نطاق + شهادة TLS + شبكة معزولة عن Production) | لا VPS/نطاق مفوّض. المكدس المحلي اُختبر (صورة API + صحة + إعادة تشغيل + volume). الشهادة العامة لم تُختبر | OPS-T-03 قبول HTTPS الحي · DEP-WEB-01 | توفير مضيف staging + شهادة بيئة + تفويض تشغيل | نتائج H1–H7 + `/health/ready` على المضيف العام | `BLOCKED BY EXTERNAL HOST` |
| DEP-G0-01 | إغلاق بوابة G0 / ESP-T-01 | موافقة `APPROVE TASK CLOSURE` غير صادرة؛ قياسات معلّقة | ESP-T-01 · G0 | سد الاعتماديات العتادية المطلوبة + موافقة مالك بالصيغة المعتمدة | بطاقات T-01 `[x]` بعد الموافقة فقط | `NOT CLOSED` |
| DEP-G1-01 | اجتياز G1 (G1-01…G1-10) | أدلة إلزامية ناقصة؛ قرارات العقد ≠ اجتياز البوابة | INT-T-04 · G1 | شهادة/عتاد حرج حسب البنود + محضر ثلاثي | `G1 READY FOR APPROVAL` ثم اعتماد صريح | `BLOCKED` |
| DEP-G2-01 | قبول نهائي AC-T-01…AC-T-25 | يعتمد على G1 وميداني | INT-T-05 · G2 | G1 معتمد + بيئات staging/ميداني | مصفوفة القبول كاملة | `NOT CLOSED` |
| DEP-BE-MUTED | كتابة `MutedAt` في مسار الكتم | فجوة تنفيذ موثّقة؛ ليست نقص عقد `alarmPhase` | BE-T-05 / `SS-P4-040` | ضمن إغلاق BE-T-05 لاحقاً | اختبار يثبت تعبئة `muted_at` عند MUTE دون إنهاء الحادثة | `DOCUMENTED GAP — NOT IN THIS PHASE` |

---

## ملاحظات تشغيل

1. نجاح/إغلاق INT-T-01 أو OPS-T-01 **لا يغلق** G0 ولا G1 ولا يفعّل MQTT الإنتاجي.
2. التكامل المحلي المعزول مسموح لأغراض التطوير/الاختبار بشهادات اختبار؛ **ممنوع** نقلها إلى الإنتاج.
3. عند استئناف أي بند أعلاه: حدّث حالته هنا أولاً، ثم اربط الدليل في `G0-decision.md` أو `G1-evidence.md` حسب البوابة — دون تضخيم عدّاد الـ50.
