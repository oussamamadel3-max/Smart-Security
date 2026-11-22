# INT-T-01 — جرد العقود ومصفوفة التوافق

تاريخ: 2026-11-21 · حالة المسودة: `DRAFT / UNDER REVIEW` · G1: `BLOCKED`

## أ) جرد عقود MQTT v1

| الرسالة | المرجع الأساسي | إصدار | اعتماد | متطلبات مرتبطة | منتج | مستهلك | تنفيذ مطابق | اختبار توافق | اعتماديات متبقية |
|---------|----------------|-------|--------|----------------|------|--------|-------------|--------------|------------------|
| STATE | `docs/contracts/mqtt/v1/state.schema.json` | 1 | DRAFT | FR-015 مسار حالة؛ NFR-004 | ESP | BE | BE parser + ESP wire جزئي | Schema samples + ingest tests | عتاد تسلسل؛ عميل Flutter حي |
| HEARTBEAT | `heartbeat.schema.json` | 1 | DRAFT | مهلة اتصال D-03 اختبار | ESP | BE | نعم (BE) | Schema + ingest | قيمة إنتاج 60د |
| EXECUTE_COMMAND | `execute-command.schema.json` | 1 | DRAFT | FR-010…018 | BE | ESP | BE builder؛ ESP استقبال برمجي | Schema + Outbox اختبارات | نشر إنتاج؛ عتاد |
| COMMAND_ACK | `command-ack.schema.json` | 1 | DRAFT | D-09؛ FR-015 | ESP | BE | BE parser؛ ESP بناء برمجي | Schema + ACK tests | ترتيب ACK/STATE → INT-T-02؛ عتاد |
| ALARM_EVENT | `alarm-event.schema.json` | 1 | DRAFT | FR-021/022؛ G1-CD-1 | ESP | BE | BE ingest + alarmPhase | Schema + PostgreSQL | عودة حساس على عتاد |
| EVENT_PERSISTED | `event-persisted.schema.json` | 1 | DRAFT | FR-021؛ G1-CD-2 | BE | ESP | BE Outbox wire | Schema | حذف محلي على جهاز حي |
| OWNERSHIP_SYNC | `ownership-sync.schema.json` | 1 | DRAFT | FR-006/034 | BE | ESP | BE builder | Schema + ownership HTTP | إقرار جهاز حي |
| EVENT_LOSS_REPORT | `event-loss-report.schema.json` | 1 | DRAFT | فقد أحداث | ESP | BE | BE parser | Schema | سعة طابور مقاسة |
| DEVICE_FAULT | `device-fault.schema.json` | 1 | DRAFT | FR-025 جزئي؛ S1 عرض | ESP | BE | BE parser | Schema | فهرس faultCode غير مجمّد |
| SETTINGS_SYNC | — | — | OUT OF v1 | G1-CD-3 | — | — | لا | اختبار «لا schema» | موافقة نطاق لاحقة |
| آلات الحالات | `docs/contracts/state-machines.md` | مسودة | DRAFT | §4.3 خطة | الكل | الكل | BE يعكسها | مراجعة وثائقية | محضر ثلاثي |
| OpenAPI HTTP | `docs/contracts/http/openapi.json` | مسودة | DRAFT | §10.3 | BE | Mobile/Web | جزئي | خارج INT-T-01 | INT-T-03 |

---

## ب) مصفوفة توافق عبر المشاريع

| البند | المرجع | العقد الحالي | التنفيذ الحالي | الاختبار | الفجوة | المسؤول | المهمة |
|-------|--------|--------------|----------------|----------|--------|---------|--------|
| مصدر الحقيقة | SRS / AGENTS | STATE+ACK | BE + عملاء نطاقياً | وحدات/تكامل BE | حي MQTT إنتاج معطّل | BE/ESP | G1 / BE-T-05 |
| alarmPhase | G1-CD-1 | ALARM_EVENT/EVENT_PERSISTED | BE منفّذ | Schema 32 + ingest | إرسال ESP حي | ESP | ESP-T-07 / G1 |
| إقرار مراحل | G1-CD-2 | EVENT_PERSISTED+phase | BE Outbox | تكامل BE | جهاز يحذف بعد المطابقة | ESP | ESP-T-05/07 |
| SETTINGS_SYNC | G1-CD-3 | لا schema | لا | Unresolved_Message_Types test | متعمد | Integration | خارج v1 |
| حقول ACK اختيارية | G1-CD-4 | schema اختياري | ParseCommandAck | Schema optional sample | Firmware يرسلهما دائماً لاحقاً | ESP | ESP-T-07 |
| رفض بلا وقت | G1-CD-5 | قاعدة FR-017 | منطق ESP برمجي | host جزئي | عتاد | ESP | DEP-HW-06 |
| قيم مؤقتة | G1-CD-6 / D-03 | موثّقة TEST | ApprovedDeviceSettings | مراجعة | قياس إنتاج | ESP/BE | DEP-HW-04 |
| stateSequence | G1-CD-7 / D-05 | مبدأ + مسودة | NVS برمجي | host | قطع طاقة | ESP | DEP-HW-05 |
| MQTT إنتاج | G1-CD-8 | معطّل | NoOp / flags false | أدلة محلية فقط | شهادة+تفويض | Ops/BE | DEP-OPS-01 |
| S1 سلامة | مالك 2026-11-21 | تمثيل بـ DISARMED/UNKNOWN/FAULT/DEVICE_FAULT | غير مثبت عتادياً | وثائقي | تمييز سبب DISARMED؛ تحقق | ESP+UI | DEP-SAFE-01 |
| ملكية PENDING/ACTIVE | SRS / AGENTS | OWNERSHIP_SYNC | BE T-03 | HTTP tests | إقرار جهاز حي | ESP/BE | G1-09 |
| D-04 تدوير يدوي | مالك | وثائقي | جزئي SoftAP/ACL اختبار | ACL 7/7 نصي | إجراء إداري مكتوب+دخان جهازين | Ops/ESP/BE | DEP + OPS |
| MutedAt | DB عمود موجود | ليس مرحلة alarmPhase | بلا كاتب كتم | — | تنفيذ مسار MUTE | BE | BE-T-05 SS-P4-040 |
| وثيقة 2026-11-15 «مخططات متميزة» | تاريخي | مستبدل بـ alarmPhase | — | — | عُولج بإحالة | Integration | تم |
| Flutter لا MQTT | AGENTS | HTTPS فقط | Mobile/Web عميل | وحدات عميل | حي Backend | Mobile/Web | EXT |
| سلامة محلية | SRS | خارج MQTT | ESP core مبني | host 100 | عتاد صفارة | ESP | G0/T-02 |
| DEVICE_FAULT ناشر ESP | مسودة schema | موجود | parser BE فقط؛ **لا** `device_fault_json` في firmware | Schema samples | ناشر firmware | ESP | ESP-T-05/07 — **غير مانع** لـ INT-T-01 |
| Outbox OWNERSHIP_SYNC | ownership-sync schema | builder BE ينشئ الصف | `OutboxDispatchService` لا ينشر `PUBLISH_OWNERSHIP_SYNC` حياً بعد | HTTP/ownership tests | مسار broker حي | BE | BE-T-05/07 — **غير مانع** لمسودة INT-T-01 |
| عرض S1 في Flutter | HARDWARE S1 | DISARMED/UNKNOWN/FAULT | pills مستقلة؛ لا قمع «محمي» عند FAULT+ARMED | وحدات عميل جزئية | قاعدة عرض مركّبة | Mobile/Web | DEP-SAFE-01 — **غير مانع** لـ INT-T-01 |

---

## ج) تعارضات مكتشفة في INT-T-01

| # | التعارض | المعالجة في هذه المهمة |
|---|---------|-------------------------|
| 1 | إحالة D-05 إلى «بند 6» بدلاً من G1-CD-7 | صُحّحت في HARDWARE-DECISIONS |
| 2 | اشتراط يبدو أنه يمنع تجهيز شهادة قبل G1 بينما G1 تحتاج دليل TLS | توضيح طبقات TLS في G1-decision / DEP-OPS-01 (تجهيز دليل ≠ تفعيل إنتاج) |
| 3 | S1 بلا تمييز عقدي لسبب DISARMED | فجوة مسجّلة؛ لا حقل جديد دون موافقة |
| 4 | وجود مسودات بلا ملكية Integration رسمية | وثائق `integration/contracts/*` + تحديث README العقود |

لا تعارض يتطلب قرار مالك جديد لإكمال مسودة INT-T-01 القابلة للمراجعة.

## د) إضافة مراجعة مشتركة 2026-11-22

محضر: [`INT-T-01-CROSS-PROJECT-REVIEW.md`](./INT-T-01-CROSS-PROJECT-REVIEW.md) · تقرير: [`../../docs/reports/integration/INT-T-01-CLOSURE-REVIEW.md`](../../docs/reports/integration/INT-T-01-CLOSURE-REVIEW.md)

مراجعة تقنية بواسطة AI Agents (ESP / Backend / Flutter): `APPROVE_WITH_GAPS`. مالك المشروع 2026-11-22 اعتمدها مع موافقته كبديل إجرائي وأغلق INT-T-01 — انظر `INT-T-01-CLOSURE-REPORT.md`. توقيعات أفراد مستقلة **غير مسجّلة**. فجوات Flutter (`healthCode`، حراس Mobile) → Mobile R-12/R-13 · WEB-EXT-10/11 · `DEP-FLUT-01/02`.
