# INT-T-01 — مسودة عقد MQTT v1 وآلات الحالات (ملكية Integration)

| الحقل | القيمة |
|--------|--------|
| المهمة | `INT-T-01` / `T-01` |
| التاريخ | 2026-11-21 |
| حالة الوثيقة | **`DRAFT ACCEPTED`** (إغلاق INT-T-01 بتاريخ 2026-11-22) — ليست عقد إنتاج |
| حالة بوابة G1 | **`BLOCKED`** — قبول المسودة لا يغلق البوابة |
| SRS | مكمل `FR-006` `FR-021` `FR-034` · `NFR-002` `NFR-004` `NFR-005` |
| اعتماديات البدء | مسودتا `D-04`/`D-05` في `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md` (متوفرتان) |
| المخططات والعينات | [`../../docs/contracts/mqtt/v1/`](../../docs/contracts/mqtt/v1/) |
| آلات الحالات | [`../../docs/contracts/state-machines.md`](../../docs/contracts/state-machines.md) |
| قرارات العقد الثمانية | [`../../docs/gates/G1-contract-decisions.md`](../../docs/gates/G1-contract-decisions.md) |
| قرارات المالك | [`../../docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md`](../../docs/reports/integration/G0-G1-OWNER-DECISIONS-REPORT.md) |

هذه الوثيقة **توحّد ملكية Integration** على المسودة الموجودة دون إعادة إنشاء مخططات متعارضة. التفاصيل النهائية وترتيب ACK/STATE الموسَّع وOpenAPI تُحال إلى `INT-T-02` / `INT-T-03` / `INT-T-04`.

---

## 1. القنوات وACL الدلالية

| Topic | الناشر | المشترك | QoS (مسودة) | Retained |
|-------|--------|---------|-------------|----------|
| `devices/{deviceId}/state` | الجهاز | الخادم | `HEARTBEAT` = 0؛ `STATE` المؤثرة: اتجاه QoS 1 في المسودة / إثبات إعادة التسليم جزئي على وسيط اختبار | لا retained لأوامر |
| `devices/{deviceId}/events` | الجهاز | الخادم | 1 | — |
| `devices/{deviceId}/acks` | الجهاز | الخادم | 1 | — |
| `devices/{deviceId}/commands` | الخادم | الجهاز | 1 لـ `EXECUTE_COMMAND` / `OWNERSHIP_SYNC` / `EVENT_PERSISTED` | **ممنوع** للأوامر اللحظية |

**ACL دلالية (ليست ملف Mosquitto إنتاجي):** كل جهاز ينشر/يشترك في قنوات `deviceId` الخاص به فقط؛ الخادم (`srv`) ينشر على `commands` ويشترك في state/events/acks. رفض مجهول ورفض عبور الأجهزة — أدلة اختبار معزولة في `G1-evidence.md`؛ إعداد Compose الرسمي = `OPS-T-01`.

**الهوية:** `deviceId` في الحمولة = مقطع الموضوع = هوية الاعتماد. رمز الرفض عند الاختلاف: `DEVICE_IDENTITY_MISMATCH`.

---

## 2. أنواع الرسائل في v1 (schemaVersion = 1)

| messageType | مخطط | منتج | مستهلك | ملاحظات |
|-------------|------|------|--------|---------|
| `STATE` | `state.schema.json` | ESP | Backend | مصدر حقيقة مع ACK؛ `clockTrusted` إلزامي |
| `HEARTBEAT` | `heartbeat.schema.json` | ESP | Backend | يحدّث `last_seen_at` فقط |
| `EXECUTE_COMMAND` | `execute-command.schema.json` | Backend | ESP | لا يغيّر الحماية وحده؛ بلا retained |
| `COMMAND_ACK` | `command-ack.schema.json` | ESP | Backend | لقطة فعلية + `resultStateSequence` (D-09) |
| `ALARM_EVENT` | `alarm-event.schema.json` | ESP | Backend | `alarmPhase` ACTIVE/ENDED؛ نفس `eventUid` |
| `EVENT_PERSISTED` | `event-persisted.schema.json` | Backend | ESP | `stored=true` بعد commit؛ إقرار مرحلة |
| `OWNERSHIP_SYNC` | `ownership-sync.schema.json` | Backend | ESP | بلا هوية مالك؛ `isLinked` |
| `EVENT_LOSS` / `EVENT_LOSS_REPORT` | `event-loss-report.schema.json` | ESP | Backend | علامة فقد عند الامتلاء/الانقطاع |
| `DEVICE_FAULT` | `device-fault.schema.json` | ESP | Backend | `faultCode` نص مفتوح |

**خارج v1 صراحةً:** `SETTINGS_SYNC` · OTA · أي `ALARM_ENDED` كـ `messageType` مستقل.

---

## 3. الحقول المشتركة والدلالات الملزمة

- إلزامي عام حيث ينطبق: `schemaVersion`, `deviceId`, `messageId`, `messageType`.
- رسائل الجهاز: `bootId` مطلوب. رسائل الخادم الثلاثة أعلاه بلا `bootId` (تعارض موثّق مقصود في المسودة الحالية).
- **مصدر الحقيقة:** آخر حالة أكدها الجهاز عبر `STATE` / `COMMAND_ACK` بتسلسل أحدث. HTTP 202 وPUBACK ليسا تنفيذاً.
- **عدم التراجع:** `stateSequence` / `resultStateSequence` الأقدم لا يرجّع `device_states`.
- **التكرار:** بالمعرفات (`messageId`, `commandId`, `(device_id, event_uid)`).
- **`clockTrusted`:** عند `false` لا يُعتمد وقت الوقوع كدقيق؛ لا يُحسب «تأخر» مزعوم؛ الأوامر البعيدة التي تحتاج تحقق `expiresAt` لا تُنفَّذ على الجهاز (`FR-017`) بينما الإنذار المحلي يستمر. في `STATE` الحقل إلزامي؛ في `COMMAND_ACK` اختياري في v1 وغيابه ليس إثبات ثقة.
- **ACK Timeout ≠ Command TTL:** مهلة انتظار الإقرار على الخادم منفصلة عن صلاحية الأمر (`expiresAt` / TTL التسجيل). قيم 10ث / 30ث / 60د / ~ساعة = تطوير فقط (`G1-CD-6` / D-03).
- **استمرار التسلسل:** مبدأ `G1-CD-7` / D-05؛ إثبات قطع الطاقة معلّق.
- **الملكية:** ربط أولي `PENDING`؛ نقل → `ACTIVE` فوري للجديد مع احتجاز النشر حتى إقرار الإصدار.

---

## 4. آلات الحالات الست

المرجع الكامل: `docs/contracts/state-machines.md`.

| الآلة | القيم الأساسية |
|-------|----------------|
| اتصال | `ONLINE`/`OFFLINE` محسوبة من `last_seen_at` + مهلة نبضة (ليست عمود `is_online`) |
| حماية | `ARMED` \| `DISARMED` \| `UNKNOWN` |
| صفارة | `OFF` \| `ALARM` \| `TEST` \| `UNKNOWN` |
| عتاد | `HEALTHY` \| `FAULT` \| `UNKNOWN` |
| أمر | `PENDING` \| `EXECUTED` \| `REJECTED` \| `TIMED_OUT` \| `CANCELLED` (على الخادم؛ ACK السلكي `EXECUTED`/`REJECTED` فقط) |
| ملكية | `PENDING` \| `ACTIVE` \| `ENDED` \| `CANCELLED` |

### S1 ضمن آلات الحماية/العتاد (بلا حقول جديدة)

انظر التفصيل في `HARDWARE-DECISIONS-D01-D07.md` §S1. ملخص عقدي:

| شرط S1 | تمثيل مسودة مسموح |
|---------|-------------------|
| تخزين سليم بعد إقلاع طبيعي | استعادة `armState` الموثوق السابق عبر مسار الجهاز ثم `STATE` |
| تعذر استعادة موثوقة | تشغيلياً غير مسلّح؛ لا `ARMED` تلقائي؛ عرض غير «محمي مؤكد» عبر `UNKNOWN`/`FAULT` و/أو `DEVICE_FAULT` |
| تدخل المالك قبل العودة الطبيعية | أمر/إجراء صريح لاحقاً — ليس جزءاً من رسالة سحرية جديدة |

**فجوة:** لا تمييز دلالي إلزامي في المخطط بين `DISARMED` اختياري و`DISARMED` استرداد — مسجّلة للإحالة إلى G1 دون اختراع حقل في INT-T-01.

---

## 5. سياسة QoS / إعادة المحاولة / التكرار

| البند | المسودة |
|-------|---------|
| أوامر لحظية | QoS 1 · `retain=false` |
| HEARTBEAT | QoS 0 |
| إعادة محاولة Outbox | على الخادم؛ النشر ≠ تنفيذ |
| تكرار وارد | إزالة تكرار بالمعرفات في الخدمات؛ المخطط لا يفرض الأحادية وحده |
| أمر منتهٍ | لا يُنشر |

إثباتات broker الجزئية: `docs/gates/G1-evidence.md`. الإنتاج يبقى معطّلاً.

---

## 6. بطاقة SETTINGS_SYNC (حسم v1)

| السؤال | الجواب المعتمد |
|--------|----------------|
| هل v1 تستخدم `SETTINGS_SYNC`؟ | **لا** — خارج v1 (`G1-CD-3`، مالك 2026-11-16 و2026-11-21) |
| قناة تهيئة بديلة في v1؟ | تهيئة Wi-Fi محلية (SoftAP / مسار جهاز) حسب D-04/`ESP-T-08` — **ليست** MQTT `SETTINGS_SYNC` |
| OTA | مفصول ومؤجّل (`D-08`) |
| إن لزم لاحقاً | مخطط + مراجعة G1 بعد موافقة نطاق — خارج INT-T-01 |

---

## 7. اعتماديات D-04 / D-05 / S1 (لا تجميد)

| بند | وضع المسودة |
|-----|-------------|
| D-04 | تدوير يدوي v1 معتمد؛ ACL/هويتين تحتاج OPS/عتاد للقبول الحي |
| D-05 / G1-CD-7 | مبدأ استمرار التسلسل معتمد؛ حجم دفعة NVS وقطع الطاقة `OPEN` |
| S1 | سياسة معتمدة؛ تحقق عتادي معلّق؛ فجوة تمييز العرض موثّقة أعلاه |
| D-01 قطع / D-02 أرقام / D-03 TTL إنتاج | غير مثبتة كإنتاج في هذا العقد |

---

## 8. ما يُحال إلى مهام لاحقة

| موضوع | المهمة |
|-------|--------|
| أمثلة ترتيب ACK/STATE موسَّعة وقواعد تحديث مفصّلة | **INT-T-02** → [`D-09-COMMAND-ACK-AND-STATE.md`](./D-09-COMMAND-ACK-AND-STATE.md) · [`samples/ordering/`](../../docs/contracts/mqtt/v1/samples/ordering/) |
| OpenAPI HTTP المشترك | INT-T-03 |
| محضر ثلاثي + اعتماد G1 | INT-T-04 |
| Compose/ACL رسمي | OPS-T-01 ✓ (مغلق 2026-11-22؛ محلي فقط) |
| كتابة `MutedAt` | BE-T-05 / `SS-P4-040` (ليست نقص عقد `alarmPhase`) |

---

## 9. مراجعة وإغلاق INT-T-01

- مراجعات تقنية مستقلة (AI) من منظور ESP/Backend/Flutter: `APPROVE_WITH_GAPS` — المحضر `INT-T-01-CROSS-PROJECT-REVIEW.md`.
- مالك المشروع 2026-11-22: اعتمد المراجعات التقنية مع موافقته الصريحة كبديل إجرائي كافٍ لشرط المراجعة المشتركة ضمن نطاق المسودة، وأصدر `APPROVE TASK CLOSURE: INT-T-01`.
- **لم تُسجَّل** توقيعات بشرية مستقلة لأسماء أفراد لم تصدر.
- التقرير: `docs/reports/integration/INT-T-01-CLOSURE-REPORT.md`.
- الفجوات المؤجّلة تبقى في `G0-G1-remaining-dependencies.md` ومهام المنتجات — لا تُعتبر مكتملة بهذا الإغلاق.
