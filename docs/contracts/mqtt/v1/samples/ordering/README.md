# أمثلة ترتيب COMMAND_ACK / STATE — INT-T-02

**ليست** اعتماد G1. كل ملف JSON هنا رسالة واحدة صالحة وفق مخطط v1 المناسب (`state` أو `command-ack`).  
القواعد السردية ونتائج الخادم المتوقعة: [`../../../../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md`](../../../../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md).

معرّفات ثابتة للسيناريوهات:

| رمز | قيمة |
|-----|------|
| `deviceId` | `11111111-1111-4111-8111-111111111111` |
| `commandId` (حيث ينطبق) | `22222222-2222-4222-8222-222222222222` |

## الفهرس

| ملف | سيناريو | الدور |
|-----|---------|--------|
| `S01-state-seq10.json` | S01 | STATE قبل ACK |
| `S01-ack-seq11.json` | S01 | ACK أحدث |
| `S02-ack-seq11.json` | S02 | ACK أولاً |
| `S02-state-seq11-duplicate.json` | S02 | STATE مساوٍ بعد ACK |
| `S03-ack-only-seq5.json` | S03 | ACK دون STATE وسيط (فوق خط أساس أقدم مفترض) |
| `S04-state-seq10.json` | S04 | خط أساس أحدث |
| `S04-ack-stale-seq7.json` | S04 | ACK متأخر التسلسل |
| `S05-ack-executed-seq12.json` | S05 | أول إغلاق |
| `S05-ack-duplicate-seq12.json` | S05 | تكرار بعد الإغلاق |
| `S06-ack-late-after-timeout-seq15.json` | S06 | LATE_ACK بتسلسل أحدث |
| `S07-state-reconcile-after-timeout-seq16.json` | S07 | مصالحة بـ STATE |
| `S08-ack-rejected-seq13.json` | S08 | REJECTED مع لقطة |

اختبار المخطط للملفات: `MqttDraftV1SchemaTests.Ordering_Scenario_Messages_Match_Schemas`.
