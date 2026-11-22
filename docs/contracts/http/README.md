# عقود HTTP / OpenAPI

**الحالة:** مسودة INT-T-03 — `DRAFT ACCEPTED` (مغلقة 2026-11-22)  
**ليست** اعتماد G1 ولا عقد إنتاج MQTT. مخططات استجابة OpenAPI ليست مكتملة لجميع DTOs.

| ملف | الدور |
|-----|--------|
| [`openapi.json`](./openapi.json) | تصدير Swashbuckle للمسارات **المنفَّذة** (+ تحسينات توثيق INT-T-03) |
| [`openapi.proposed.json`](./openapi.proposed.json) | بنود `proposed` رمزية فقط — لا تُستهلك كعقد حي |
| [`error-codes.md`](./error-codes.md) | جدول أخطاء ورموز ثابتة من الوسيط/الأوامر |
| [`samples/`](./samples/) | أمثلة JSON بلا أسرار |
| نظرة Integration | [`../../../integration/contracts/HTTP-V1-DRAFT.md`](../../../integration/contracts/HTTP-V1-DRAFT.md) |
| جرد Endpoints | [`../../../integration/contracts/HTTP-ENDPOINT-INVENTORY.md`](../../../integration/contracts/HTTP-ENDPOINT-INVENTORY.md) |

## إعادة التصدير من الخادم

من `security_backEnd/` مع قاعدة اختبار معزولة:

```text
$env:G1_WRITE_OPENAPI="1"
dotnet test API.IntegrationTests/API.IntegrationTests.csproj --filter FullyQualifiedName~OpenApiDraftExportTests
```

يتطلب `ESP32_SECURITY_TEST_DB` (أو User Secrets). لا يمس إنتاجاً.

## قواعد

1. نجاح `POST …/commands` = **202** + `PENDING` + `actualStateChanged=false`.
2. لا تخلط `openapi.proposed.json` مع العقد المنفَّذ.
3. لا تضع refresh tokens أو كلمات مرور في العينات.
