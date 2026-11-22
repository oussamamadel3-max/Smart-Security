# أخطاء HTTP المنفذة — مسودة INT-T-03

المصدر: `ErrorHandlingMiddleware` و`OwnerDeviceCommandsController` و`ExecuteCommandDraft.RejectCodes`.  
ملف `openapi.json` تصدير Swashbuckle + تحسينات توثيق INT-T-03. أخطاء الوسيط **ليست** `ProblemDetails`؛ شكلها الفعلي هو الغلاف أدناه.

غلاف الخطأ:

```json
{ "success": false, "error": { "code": "<CODE>", "message": "<نص عربي>", "meta": {} } }
```

## الحالات المطلوبة في عقد HTTP المشترك

| HTTP | متى | رمز افتراضي في الكود |
|---|---|---|
| 202 | `POST /api/owner/devices/{deviceId}/commands` بعد قبول الطلب | النجاح ليس خطأ. الجسم عبر `ApiResponse` والحالة `PENDING`. `actualStateChanged=false` |
| 201 | إنشاء مالك / جهاز / ربط أولي / تنبيه إداري | نجاح إنشاء مورد |
| 401 | لا جلسة / لا هوية | `AUTH_UNAUTHENTICATED` |
| 403 | هوية صحيحة بلا صلاحية أو بلا ملكية أو حساب غير نشط على عملية محمية | `AUTH_FORBIDDEN` ما لم يمرّر المستدعي رمزاً آخر |
| 409 | تعارض عمل: idempotency، أمر معلّق، ملكية، جهاز غير متصل (حسب المسار) | الرمز الذي يمرره المستدعي، والافتراضي `CONFLICT` |
| 503 | تبعية مطلوبة غير متاحة | الافتراضي `SERVICE_UNAVAILABLE` |

`400` و`404` و`500` موجودة أيضاً في الوسيط (`BadRequestException` / `NotFoundException` / `INTERNAL_ERROR`) وليست بديلاً عن الجدول أعلاه.

## رموز رفض الأمر (`ExecuteCommandDraft.RejectCodes`)

`DEVICE_OFFLINE`, `OWNERSHIP_NOT_CONTROLLABLE`, `COMMAND_CONFLICT`, `AWAITING_RECONCILIATION`, `MUTE_TARGET_REQUIRED`, `MUTE_TARGET_MISMATCH`, `TEST_DURING_ALARM`, `STOP_TEST_NOT_ACTIVE`, `UNSUPPORTED_COMMAND_TYPE`, `IDEMPOTENCY_CONFLICT`, `COMMAND_EXPIRED`, `OWNERSHIP_VERSION_STALE`, `OWNERSHIP_TRANSFERRED`, `AGGREGATE_MISSING`, `MQTT_OUTBOX_DISABLED`, `MQTT_PUBLISH_FAILED`.

تجميد القاموس النهائي لإنتاج G1 يبقى ضمن `INT-T-04`؛ هذه الأسماء **منفَّذة حالياً** وموثَّقة لمسودة INT-T-03.

## Idempotency

`CreateOwnerDeviceCommandRequestDto.IdempotencyKey` مطلوب على إنشاء الأمر. إعادة نفس المفتاح ونفس الطلب تعيد الأمر القائم. اختلاف الحمولة لنفس المفتاح ينتج تعارض `IDEMPOTENCY_CONFLICT`.

## قنوات الجلسة

| قناة | العميل | Refresh |
|------|--------|---------|
| `body` | `security_mobile` | في جسم JSON؛ يُخزَّن بأمان على الجهاز |
| `cookie` | `admin_web` | HttpOnly + CSRF/Origin على `/api/auth/refresh`؛ `refreshToken` في الجسم = null |

## دور مقابل ملكية

- دور `OWNER` يلزم لمسارات `/api/owner/*` لكنه **لا** يكفي وحده: كل عملية جهاز تتحقق من ملكية الطالب للجهاز.
- مسارات `/api/admin/*` تستخدم `IPermissionGate` بمفاتيح مثل `Devices.Manage` و`Users.View` — منفصلة عن عزل ملكية المالك.

## ما لا يدّعيه هذا الملف

- نجاح `202` لا يعني تنفيذ `ARM` أو `DISARM` على الجهاز.
- اعتماد G1 أو تفعيل MQTT الإنتاجي.
- اكتمال كل مخططات استجابة OpenAPI لكل حقل DTO.
