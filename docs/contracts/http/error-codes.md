# أخطاء HTTP المنفذة — ليست قائمة G1 المعتمدة

المصدر: `ErrorHandlingMiddleware` و`OwnerDeviceCommandsController` و`ExecuteCommandDraft.RejectCodes`. ملف `openapi.json` هو تصدير Swashbuckle من مضيف الاختبار. عملية إنشاء الأمر معلّمة `[ProducesResponseType(202)]` لأن `IResult` كان يظهر كـ `200` في المولّد رغم أن التنفيذ يعيد `202`. أخطاء الوسيط ليست `ProblemDetails`؛ شكلها الفعلي هو الغلاف أدناه، حتى لو لم يكررها ملف OpenAPI على كل مسار.

غلاف الخطأ:

```json
{ "success": false, "error": { "code": "<CODE>", "message": "<نص عربي>", "meta": {} } }
```

## الحالات المطلوبة في عقد G1

| HTTP | متى | رمز افتراضي في الكود |
|---|---|---|
| 202 | `POST /api/owner/devices/{deviceId}/commands` بعد قبول الطلب | النجاح ليس خطأ. الجسم عبر `ApiResponse` والحالة `PENDING`. `actualStateChanged=false` |
| 401 | لا جلسة / لا هوية | `AUTH_UNAUTHENTICATED` |
| 403 | هوية صحيحة بلا صلاحية أو بلا ملكية | `AUTH_FORBIDDEN` ما لم يمرّر المستدعي رمزاً آخر |
| 409 | تعارض عمل: idempotency، أمر معلّق، ملكية، جهاز غير متصل | الرمز الذي يمرره المستدعي، والافتراضي `CONFLICT` |
| 503 | تبعية مطلوبة غير متاحة | الافتراضي `SERVICE_UNAVAILABLE` |

`400` و`404` و`500` موجودة أيضاً في الوسيط (`BadRequestException` / `NotFoundException` / `INTERNAL_ERROR`) وليست بديلاً عن الجدول أعلاه.

## رموز رفض الأمر الموجودة في `ExecuteCommandDraft.RejectCodes`

`DEVICE_OFFLINE`, `OWNERSHIP_NOT_CONTROLLABLE`, `COMMAND_CONFLICT`, `AWAITING_RECONCILIATION`, `MUTE_TARGET_REQUIRED`, `MUTE_TARGET_MISMATCH`, `TEST_DURING_ALARM`, `STOP_TEST_NOT_ACTIVE`, `UNSUPPORTED_COMMAND_TYPE`, `IDEMPOTENCY_CONFLICT`, `COMMAND_EXPIRED`, `OWNERSHIP_VERSION_STALE`, `OWNERSHIP_TRANSFERRED`, `AGGREGATE_MISSING`, `MQTT_OUTBOX_DISABLED`, `MQTT_PUBLISH_FAILED`.

هذه أسماء كود حالية. تجميدها كقاموس أخطاء نهائي لـ G1 يحتاج مراجعة `INT-T-03` ولم يتم هنا.

## Idempotency

`CreateOwnerDeviceCommandRequestDto.IdempotencyKey` مطلوب على إنشاء الأمر. إعادة نفس المفتاح ونفس الطلب تعيد الأمر القائم. اختلاف الحمولة لنفس المفتاح ينتج تعارض `IDEMPOTENCY_CONFLICT`.

## ما لا يدّعيه هذا الملف

- لا يوجد عميل Flutter أو Web في المستودع يستهلك هذا العقد.
- نجاح `202` لا يعني تنفيذ `ARM` أو `DISARM` على الجهاز.
