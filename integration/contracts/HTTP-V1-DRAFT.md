# مسودة عقد HTTP / OpenAPI v1 — INT-T-03

| الحقل | القيمة |
|--------|--------|
| الحالة | `DRAFT ACCEPTED` (INT-T-03 مغلقة 2026-11-22) — **ليست** اعتماد G1 |
| المصدر التنفيذي | `security_backEnd` Controllers + DTOs + `ErrorHandlingMiddleware` |
| تصدير OpenAPI | [`docs/contracts/http/openapi.json`](../../docs/contracts/http/openapi.json) (Swashbuckle) |
| الجرد التفصيلي | [`HTTP-ENDPOINT-INVENTORY.md`](./HTTP-ENDPOINT-INVENTORY.md) |
| أخطاء | [`docs/contracts/http/error-codes.md`](../../docs/contracts/http/error-codes.md) |
| عينات | [`docs/contracts/http/samples/`](../../docs/contracts/http/samples/) |
| G1 | **ليست** اعتماد G1 — الاعتماد عبر `INT-T-04` |

## 1. مبادئ ملزمة (قرارات G1 الثمانية + D-09)

1. مصدر الحقيقة = آخر حالة مؤكدة من الجهاز (`ACK` / `STATE`) — ليس HTTP ولا MQTT PUBACK.
2. `POST …/commands` → **HTTP 202** + `status=PENDING` + `actualStateChanged=false` عند القبول.
3. لا يُستنتج نجاح `ARM`/`DISARM` من قبول الطلب.
4. الملكية الأولية `PENDING` ≠ `ACTIVE` بعد النقل؛ نشر الأمر ينتظر مزامنة إصدار الملكية عند اللزوم.
5. دورة الحادثة على HTTP تستخدم حقل `status` (`ACTIVE`/`ENDED`)؛ `alarmPhase` خاص بمسار MQTT — الكتم ≠ إنهاء الحادثة.
6. `SETTINGS_SYNC` وOTA خارج v1.
7. MQTT الإنتاجي يبقى **DISABLED**.

## 2. غلاف الاستجابة الموحد

نجاح:

```json
{ "success": true, "message": "<نص اختياري>", "data": { }, "meta": { } }
```

فشل (`ErrorHandlingMiddleware` — ليس `ProblemDetails`):

```json
{ "success": false, "error": { "code": "<CODE>", "message": "<عربي>", "meta": { } } }
```

## 3. المصادقة والجلسة (Backend T-02 المنفّذ)

| مسار | ملاحظة |
|------|--------|
| `POST /api/auth/login` | `refreshDelivery`: `body` (Mobile) أو `cookie` (Web) |
| `POST /api/auth/refresh` | إما جسم `refreshToken` **أو** كوكي HttpOnly + CSRF/Origin — لا الاثنين معاً |
| `POST /api/auth/logout` | جلسة واحدة |
| `POST /api/auth/logout-all` | كل الجلسات |
| `GET /api/auth/me` | هوية حالية |

- Web: Access في الذاكرة؛ Refresh في كوكي HttpOnly؛ CSRF موقّع.
- Mobile: Access + Refresh في تخزين آمن؛ لا كوكي جلسة.
- حساب معطّل/مؤرشف → رفض مصادقة أو عمليات محمية (`is_active`).

## 4. التصنيف: منفّذ مقابل مقترح

### 4.1 منفّذ (`implemented`) — 37 عملية

انظر الجرد الكامل. يغطي خطة §4.2 عملياً: هوية، حسابات مالك، أجهزة (تسجيل/ربط/نقل/فك/أرشفة)، قائمة/تفاصيل مالك، أوامر 202+idempotency، أحداث إنذار، أعطال إدارية، سجلات تدقيق، push tokens، تنبيه إداري.

تصدير OpenAPI الحالي يطابق **مسارات/طرق** التنفيذ؛ فجواته الرئيسية: غياب مخططات الاستجابة/الغلاف، توثيق ناقص لـ 401/403/409/503، ووسم 200 بدل 201 لأربعة مسارات إنشاء.

### 4.2 مقترح (`proposed`) — غير منفّذ كمسارات HTTP مستقلة

لا مسارات HTTP مطلوبة في §4.2 غائبة كلياً عن Controllers. البنود التالية **مقترحة/مؤجّلة** وليست جزءاً من العقد الجاهز للإنتاج:

| معرف | الوصف | سبب الوسم `proposed` |
|------|--------|----------------------|
| `PROP-HTTP-01` | ترقيم/فرز موحّد لقوائم المالك (أجهزة/أوامر/أحداث) بنفس نموذج `PagedResultDto` الإداري | القوائم الحالية مصفوفات كاملة |
| `PROP-HTTP-02` | مسار مالك مخصّص لتفاصيل `DEVICE_FAULT` منفصل عن تضمين الجهاز | العطل يظهر جزئياً في تفاصيل الجهاز؛ الاستهلاك في Flutter جزئي |
| `PROP-HTTP-03` | حقول استجابة OpenAPI كاملة لكل DTO + `security` لكل عملية | تحسين توثيق؛ السلوك موجود في الكود |
| `PROP-HTTP-04` | أي مسار `SETTINGS_SYNC` / OTA عبر HTTPS | خارج v1 (G1-CD-3) |

ملف إضافي: [`docs/contracts/http/openapi.proposed.json`](../../docs/contracts/http/openapi.proposed.json) — هيكل رمزي فقط، **لا** يُستهلك كعقد منفّذ.

## 5. الأوامر (عقد القبول الحرج)

طلب:

```json
{
  "type": "ARM",
  "idempotencyKey": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
  "targetAlarmEventUid": null
}
```

`type`: `ARM` | `DISARM` | `MUTE_ALARM` | `TEST_SIREN` | `STOP_TEST`  
`MUTE_ALARM` يتطلب `targetAlarmEventUid`.

استجابة 202 (مختصر): `commandId`, `status=PENDING`, `actualStateChanged=false`, تسلسلات، `expiresAt`. إعادة نفس `idempotencyKey` بنفس الحمولة = إعادة تشغيل آمنة؛ اختلاف الحمولة = `IDEMPOTENCY_CONFLICT` (409).

## 6. الملكية والصلاحيات

| طبقة | الآلية |
|------|--------|
| دور | `OWNER` لمسارات `/api/owner/*`؛ إداري عبر `[Authorize]` + `IPermissionGate` |
| ملكية جهاز | فلترة استعلام + تحقق على العملية؛ لا يكفي اسم الدور وحده |
| PENDING | رؤية محدودة؛ لا تحكم كامل حتى ACTIVE + مزامنة إصدار عند الحاجة |
| نقل | مالك جديد ACTIVE على الخادم فوراً؛ نشر الأمر ينتظر إقرار الجهاز لإصدار الملكية |

## 7. توافق العملاء

| عميل | الحكم التقني (INT-T-03) |
|------|-------------------------|
| `security_mobile` | متوافق على الهوية (body) والأوامر (202≠تنفيذ) والأجهزة/الأحداث |
| `admin_web` | متوافق على الهوية (cookie+CSRF) والأوامر والمسارات الإدارية المستخدمة |
| `smart_security_app_core` | مسارات هوية + رموز حالة فقط؛ DTOs الأجهزة في التطبيقات |

التفاصيل: [`INT-T-03-CROSS-PROJECT-REVIEW.md`](./INT-T-03-CROSS-PROJECT-REVIEW.md).

## 8. ما لا يدّعيه هذا الملف

- اعتماد G1 / إنتاج MQTT / إغلاق G0/G2.
- تطابق بكسل أو تشغيل محاكي Flutter.
- اكتمال مخططات استجابة Swashbuckle لكل حقل.
- شرعنة فجوات خادم معروفة (`MutedAt` كتابة، Outbox إنتاج، …) — تُسجَّل كإحالات.
