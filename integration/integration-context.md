# سياق المشروع — العقود والتكامل (`integration`)

آخر تحديث: 2026-11-22 (إغلاق INT-T-03؛ 3/6؛ النظام 25/50؛ G1 BLOCKED)  
السياق العام: [`../system-context.md`](../system-context.md) · التقدّم: [`integration-progress.md`](./integration-progress.md) · HTTP: [`contracts/HTTP-V1-DRAFT.md`](./contracts/HTTP-V1-DRAFT.md) · D-09: [`contracts/D-09-COMMAND-ACK-AND-STATE.md`](./contracts/D-09-COMMAND-ACK-AND-STATE.md)

## الهوية

| الحقل | القيمة |
|--------|--------|
| المسؤولية | عقود مشتركة (MQTT/HTTP)، آلات حالات، بوابات G1/G2، مصفوفة قبول، تعريف الإنجاز |
| التقنية | وثائق وتنسيق فقط — التنفيذ في المشاريع الأخرى |
| مسار العمل | `integration/` |
| لا يملك | كود تشغيل إنتاجي · عتاد · نشر VPS |

## مبادئ ملزمة

- بدون اعتماد `G1` لا تُغلق مسارات أوامر/حوادث إنتاجية.
- قبول INT-T-01/INT-T-02/INT-T-03 التوثيقي **لا** يساوي اعتماد G1 ولا تفعيل MQTT الإنتاجي.
- مصدر الحقيقة: تأكيد الجهاز عبر STATE/ACK بتسلسل أحدث — لا HTTP/PUBACK.
- HTTP 202 / `PENDING` / `actualStateChanged=false` = قبول طلب أمر فقط.

## المهمة الجارية

- لا مهمة Integration مفتوحة للتنفيذ الآن.
- **INT-T-01** و**INT-T-02** و**INT-T-03** مغلقتان. التالية: **INT-T-04** بانتظار `START TASK: INT-T-04`.
- التفاصيل: [`integration-current.md`](./integration-current.md)

## تتبّع المهام

| مهمة | العنوان | الحالة |
|------|---------|--------|
| T-01 | مسودة عقد MQTT وآلات الحالات | **مغلقة** 2026-11-22 — DRAFT ACCEPTED |
| T-02 | توثيق D-09 وأمثلة ACK/STATE | **مغلقة** 2026-11-22 — D-09 DOCUMENTATION ACCEPTED |
| T-03 | عقد HTTP / OpenAPI مشترك | **مغلقة** 2026-11-22 — HTTP V1 DRAFT ACCEPTED |
| T-04 | بوابة اعتماد G1 | التالية — بانتظار START · G1 BLOCKED |
| T-05 | مصفوفة قبول وبوابة G2 | معلّقة |
| T-06 | تعريف إنجاز المهمة (§14) | معلّقة |

**ملخص:** **3/6** مكتملة · التالية T-04 · G1 BLOCKED

## ما أُنجز فعلياً

- INT-T-01: مسودة MQTT v1 + آلات الحالات.
- INT-T-02: D-09 + عينات ترتيب S01…S08 + قواعد `device_states` + مراجعة توافق تقنية.
- INT-T-03: مسودة HTTP/OpenAPI + جرد 37 endpoint + عينات + مراجعة Flutter/Backend؛ مخططات استجابة غير مكتملة لجميع DTOs (موثّق).
