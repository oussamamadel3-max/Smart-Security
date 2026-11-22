# INT-T-02 — توثيق D-09 وCOMMAND_ACK / STATE (ترتيب الوصول وقواعد التحديث)

| الحقل | القيمة |
|--------|--------|
| المهمة | `INT-T-02` / `T-02` |
| التاريخ | 2026-11-22 |
| حالة الوثيقة | **`DRAFT EXTENDED`** تحت ملكية Integration — ليست اعتماد G1 |
| القرار المرجعي | D-09 محسوم: `COMMAND_ACK` يحمل **الحالة الفعلية الكاملة** + `resultStateSequence` |
| SRS | `FR-015` · مصدر الحقيقة |
| المخططات | [`../../docs/contracts/mqtt/v1/`](../../docs/contracts/mqtt/v1/) |
| آلات الحالات | [`../../docs/contracts/state-machines.md`](../../docs/contracts/state-machines.md) |
| قرارات العقد | [`../../docs/gates/G1-contract-decisions.md`](../../docs/gates/G1-contract-decisions.md) §5 |
| أمثلة الترتيب | [`../../docs/contracts/mqtt/v1/samples/ordering/`](../../docs/contracts/mqtt/v1/samples/ordering/) |
| تنفيذ الخادم | `DeviceInboundTelemetryService` |

**لا يُعاد فتح** تمثيل بديل لـ D-09 (مثل تسلسل فقط بلا لقطة حالة). اعتماد الحقول النهائية ثلاثياً = `INT-T-04` / G1.

---

## 1. قرار D-09 المحسوم (بدون بدائل)

| بند | المعتمد |
|-----|---------|
| شكل ACK | `COMMAND_ACK` على `devices/{deviceId}/acks` |
| المحتوى | لقطة فعلية كاملة: `armState`, `sirenState`, `healthState`, `currentAlarmEventUid` (+ اختياريات) |
| التسلسل | `resultStateSequence` إلزامي — نفس فضاء العدّ مع `STATE.stateSequence` |
| ربط الأمر | `commandId` + `status` ∈ {`EXECUTED`,`REJECTED`} فقط على السلك |
| مصدر الحقيقة | آخر لقطة مؤكدة بجهاز بتسلسل أحدث؛ **ليس** HTTP 202 ولا MQTT PUBACK |
| الحقلان الاختياريان في v1 | `confirmedOwnershipVersion`, `clockTrusted` — الغياب ≠ إثبات |

---

## 2. قواعد تحديث `device_states` على الخادم

مرجع التنفيذ: `security_backEnd/Application/Services/MqttServices/DeviceInboundTelemetryService.cs`.

### 2.1 مدخلات تؤثّر على الحالة الفعلية

| رسالة | حقل التسلسل | يحدّث الحماية/الصفارة/التشخيص؟ | ملاحظات |
|--------|-------------|-------------------------------|---------|
| `STATE` | `stateSequence` | نعم إن `sequence > stored` | `clockTrusted` و`confirmedOwnershipVersion` إلزاميان في المخطط |
| `COMMAND_ACK` | `resultStateSequence` | نعم إن `sequence > stored` | يُغلق صف الأمر أيضاً عند تطابق الشروط |
| `HEARTBEAT` | — | لا | `last_seen_at` فقط |

### 2.2 عدم التراجع

1. إذا `incomingSequence <= device_states.state_sequence`:
   - **لا** تُعاد كتابة `armState` / `sirenState` / `healthState` / التسلسل.
   - النتيجة الاعتيادية: `IGNORED_STALE_SEQUENCE`.
   - استثناء ACK متأخر: إن وُجد أمر غير مغلق نهائياً، قد تُغلق صف الأمر فقط → `COMMAND_CLOSED_ONLY` (بدون رجوع الحالة).
2. إذا `incomingSequence > stored`: طبّق اللقطة (`APPLIED`) واضبط `state_sequence = incomingSequence`.

### 2.3 إغلاق الأمر من ACK

| وضع الأمر قبل ACK | السلوك |
|-------------------|--------|
| `PENDING` (أو بعد إرسال) | يُغلق إلى `EXECUTED`/`REJECTED`؛ يُسجَّل `AcknowledgedAt` و`ResultStateSequence` |
| `TIMED_OUT` | `LATE_ACK`: يُغلق بالنتيجة الواردة؛ `ReconciledAt`؛ **لا** يحيي `CANCELLED` |
| `EXECUTED` / `REJECTED` | تكرار — لا إعادة فتح |
| `CANCELLED` | يُتجاهل الإقرار المتأخر |

`STATE` أحدث من أمر `TIMED_OUT` غير مُصالح يضبط `ReconciledAt` بسبب مصالحة حالة (`RECONCILED_BY_STATE`) **دون** تعليم الأمر `EXECUTED`.

### 2.4 `clockTrusted` و`confirmedOwnershipVersion` على ACK

- إن **غاب** `clockTrusted` على ACK: لا يُمسّ عمود الثقة المخزَّن (`ClockTrustedSpecified=false`).
- إن **وُجد**: يُكتب القيمة الواردة.
- إن غاب `confirmedOwnershipVersion`: لا رفع لإصدار الملكية المؤكد (وليس نسخاً من `ownershipVersion`).
- إن وُجد وكانت القيمة `>` المخزَّن: يُرفع `ConfirmedOwnershipVersion` وقد يفعّل ملكية `PENDING` المطابقة.

### 2.5 ما لا يُستنتج أبداً

- نجاح `POST` أمر (`202`) أو `actualStateChanged=false` في الرد.
- PUBACK من الوسيط أو تسليم Outbox.
- غياب ACK ضمن المهلة = `TIMED_OUT` على الخادم ≠ فشل تنفيذ مؤكد على الجهاز.

---

## 3. ترتيب الوصول — مصفوفة السيناريوهات

الأمثلة الحية تحت `docs/contracts/mqtt/v1/samples/ordering/`. الملخص:

| Id | الترتيب | أثر `device_states` | أثر صف الأمر |
|----|---------|---------------------|--------------|
| S01 | `STATE(seq=N)` ثم `ACK(result=N+1)` | حالة ACK تُطبَّق | يُغلق إن طابق `commandId` |
| S02 | `ACK(result=N+1)` ثم `STATE(seq=N+1)` مكرر/مساوٍ | ACK يطبّق؛ STATE المساوي يُتجاهل للحالة | يُغلق عند ACK |
| S03 | `ACK` فقط (بعد خط أساس أقدم) | تُطبَّق لقطة ACK إذا أحدث | يُغلق |
| S04 | `STATE(seq=10)` ثم `ACK(result=7)` | لا رجوع؛ stale | قد `COMMAND_CLOSED_ONLY` إن الأمر معلّق |
| S05 | ACK مكرر بعد `EXECUTED` | لا تغيير حالة من التكرار | لا إعادة فتح |
| S06 | مهلة ثم ACK متأخر بتسلسل أحدث | تُحدَّث الحالة إن أحدث؛ وإلا إغلاق فقط | `LATE_ACK` |
| S07 | `STATE` أحدث بعد `TIMED_OUT` | تُحدَّث الحالة | مصالحة بدون `EXECUTED` |
| S08 | ACK `REJECTED` مع لقطة فعلية | تُطبَّق اللقطة إن أحدث | الأمر `REJECTED` + `rejectCode` |

**قاعدة التوحيد:** الخادم يعالج كل رسالة واردة على حدة وفق التسلسل؛ **لا** يفترض ترتيباً زمنياً بين قناتي `state` و`acks` غير ما تثبته قيم التسلسل.

---

## 4. توافق المشاريع (ملخص)

| مشروع | التوافق مع D-09 الموثّق هنا | فجوات مؤجّلة |
|--------|------------------------------|--------------|
| Backend | `ParseCommandAck` + تحديث الحالة/الأمر مطابق للقرار | MQTT إنتاجي معطّل؛ `MutedAt` → BE-T-05 |
| ESP | عقود المسودة تتوقع ACK كامل + تسلسل؛ firmware إنتاجي غير مثبت | عتاد/قطع طاقة D-05؛ ناشر DEVICE_FAULT |
| Flutter | يعرض مؤشرات من API؛ لا MQTT مباشر؛ لا يستنتج تنفيذاً من 202 | S1 UX / healthCode → DEP-FLUT-* |

محضر المراجعة التقنية: [`INT-T-02-CROSS-PROJECT-REVIEW.md`](./INT-T-02-CROSS-PROJECT-REVIEW.md).

---

## 5. ما يبقى لـ INT-T-04 / G1

- تجميد أسماء الحقول ضد تغيير بلا إصدار جديد.
- اختبارات عتاد لترتيب ACK/STATE وقطع طاقة التسلسل.
- محضر ثلاثي بشري عند الاعتماد.
- تفعيل مسارات الإنتاج.

---

## 6. إحالات خارج النطاق

| فجوة | الإحالة |
|------|---------|
| S1 تحقق عتادي / UX | DEP-SAFE-01 · DEP-FLUT-01 |
| `MutedAt` كاتب | DEP-BE-MUTED · BE-T-05 |
| OpenAPI HTTP | INT-T-03 |
| اعتماد G1 | INT-T-04 |
| Compose محلي | OPS-T-01 ✓ |
