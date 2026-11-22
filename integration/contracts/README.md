# ملكية عقود Integration

**الحالة العامة للمسودات MQTT v1:** `DRAFT ACCEPTED` (INT-T-01) + امتداد D-09 توثيقي مقبول (INT-T-02 مغلقة 2026-11-22)  
**HTTP / OpenAPI:** مسودة INT-T-03 **`DRAFT ACCEPTED`** (`HTTP-V1-DRAFT.md`؛ مغلقة 2026-11-22) — **ليست** `G1 APPROVED` ولا عقد إنتاج. اعتماد الإنتاج يبقى بوابة `INT-T-04` / `G1`.

## شجرة المراجع (نسخة واحدة فقط)

| الموضوع | المرجع الأساسي (لا تُنشأ نسخة موازية) | الوصي |
|---------|----------------------------------------|--------|
| مخططات MQTT JSON + عينات | [`../../docs/contracts/mqtt/v1/`](../../docs/contracts/mqtt/v1/) | Integration (صيانة) · Backend/ESP يستهلكان |
| نظرة قنوات/QoS/تكرار | [`../../docs/contracts/mqtt/v1/README.md`](../../docs/contracts/mqtt/v1/README.md) | Integration |
| آلات الحالات الست | [`../../docs/contracts/state-machines.md`](../../docs/contracts/state-machines.md) | Integration |
| D-09 + ترتيب ACK/STATE + قواعد `device_states` | [`D-09-COMMAND-ACK-AND-STATE.md`](./D-09-COMMAND-ACK-AND-STATE.md) | Integration (**INT-T-02**) |
| أمثلة ترتيب الوصول | [`../../docs/contracts/mqtt/v1/samples/ordering/`](../../docs/contracts/mqtt/v1/samples/ordering/) | Integration |
| مسودة OpenAPI HTTP | [`../../docs/contracts/http/`](../../docs/contracts/http/) | Integration عبر **INT-T-03** |
| وثيقة توحيد HTTP v1 | [`HTTP-V1-DRAFT.md`](./HTTP-V1-DRAFT.md) | Integration (**INT-T-03**) |
| جرد Endpoints HTTP | [`HTTP-ENDPOINT-INVENTORY.md`](./HTTP-ENDPOINT-INVENTORY.md) | Integration (**INT-T-03**) |
| مراجعة توافق INT-T-03 | [`INT-T-03-CROSS-PROJECT-REVIEW.md`](./INT-T-03-CROSS-PROJECT-REVIEW.md) | Integration |
| قرارات عقد G1 الثمانية | [`../../docs/gates/G1-contract-decisions.md`](../../docs/gates/G1-contract-decisions.md) | Integration + مالك |
| وثيقة توحيد INT-T-01 | [`MQTT-V1-DRAFT.md`](./MQTT-V1-DRAFT.md) | Integration |
| جرد + توافق INT-T-01 | [`INT-T-01-inventory-and-compatibility.md`](./INT-T-01-inventory-and-compatibility.md) | Integration |
| مراجعة توافق INT-T-02 | [`INT-T-02-CROSS-PROJECT-REVIEW.md`](./INT-T-02-CROSS-PROJECT-REVIEW.md) | Integration |

## قواعد

1. لا تنسخ مخطط JSON إلى مجلد آخر «للتنظيم» — عدّل المرجع الأساسي أو اربط إليه.
2. أي تغيير دلالي على حقل معتمد يحتاج موافقة؛ المسودة تبقى `schemaVersion=1` حتى إصدار عقد جديد عند الاعتماد.
3. `SETTINGS_SYNC` وOTA **خارج v1** (قرار G1-CD-3) — لا schema هنا.
4. اختبار صلاحية العينات: `security_backEnd/Application.Tests/MqttDraftV1SchemaTests.cs`.
