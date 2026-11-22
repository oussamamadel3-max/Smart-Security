# عينات HTTP JSON — INT-T-03

عينات تحققية متوافقة مع DTOs الحالية. بلا أسرار حقيقية.

| ملف | السيناريو |
|-----|-----------|
| `auth-login-body-success.json` | دخول Mobile (`refreshDelivery=body`) |
| `auth-login-cookie-success.json` | دخول Web (`refreshDelivery=cookie`؛ refreshToken=null في الجسم) |
| `auth-error-401.json` | فشل مصادقة |
| `command-post-request.json` | طلب أمر ARM مع idempotency |
| `command-post-202-pending.json` | قبول 202 — لا تغيّر حماية |
| `command-conflict-409.json` | تعارض أمر/idempotency |
| `alarm-event-active.json` | حادثة ACTIVE مع mutedAt اختياري |
| `device-list-item.json` | عنصر قائمة جهاز مالك |

التحقق الآلي: `security_backEnd/Application.Tests/HttpOpenApiDraftTests.cs` (+ صحة `openapi.json`).
