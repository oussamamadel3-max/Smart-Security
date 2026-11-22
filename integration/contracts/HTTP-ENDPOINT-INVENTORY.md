# جرد Endpoints HTTP — INT-T-03

مصدر الحقيقة: Controllers في `security_backEnd/API/Controllers` (37 عملية).  
مرجع العقد: `docs/contracts/http/openapi.json` (30 path / 37 method) بعد overlays INT-T-03.  
الغلاف: `ApiResponse` لجميع الردود الناجحة ما لم يُذكر خلاف ذلك.

**حالة الوثيقة:** ما بعد إغلاق INT-T-03 — الأعمدة تعكس **الوضع الحالي** للعقد المنشور، لا فروقات تاريخية قبل التصحيح.

أسطورة OpenAPI: `PATH✓` مسار موجود · `SCHEMA△` طلب و/أو استجابة جزئية · `STATUS✓` رمز النجاح يطابق التنفيذ · `RESP△` استجابة بلا `content` كامل لكل الحقول.

---

## Health

| Method | Route | منتج | مستهلك | Auth | Request | Response data | HTTP | OpenAPI |
|--------|-------|------|--------|------|---------|---------------|------|---------|
| GET | `/api/health` | Backend | Ops/مراقبة | Anonymous | — | `{}` | 200 | PATH✓ SCHEMA△ · security `[]` |

---

## Auth — منتج Backend · مستهلك Mobile+Web+Core paths

| Method | Route | Auth | Request | Response data | HTTP | Notes | OpenAPI |
|--------|-------|------|---------|---------------|------|-------|---------|
| POST | `/api/auth/login` | Anon | `LoginRequestDto` | `AuthTokensResponseDto` | 200 | `refreshDelivery` body\|cookie | PATH✓ · schema طلب + `AuthTokensResponseDto` · security `[]` |
| POST | `/api/auth/refresh` | Anon | `RefreshRequestDto?` | `AuthTokensResponseDto` | 200 | cookie **أو** body | PATH✓ · security `[]` |
| POST | `/api/auth/logout` | JWT | `RefreshRequestDto?` | `{}` | 200 | | PATH✓ |
| POST | `/api/auth/logout-all` | JWT | — | `{}` | 200 | | PATH✓ |
| GET | `/api/auth/me` | JWT | — | `MeResponseDto` | 200 | | PATH✓ · `MeResponseDto` في schemas |

---

## Owner devices / commands / alarms / notifications — دور OWNER

| Method | Route | مستهلك | Request | Response data | HTTP | Notes | OpenAPI |
|--------|-------|--------|---------|---------------|------|-------|---------|
| GET | `/api/owner/devices` | M+W | — | `OwnerDeviceListItemDto[]` | 200 | PENDING محدود | PATH✓ RESP△ |
| GET | `/api/owner/devices/{deviceId}` | M+W | — | `OwnerDeviceDetailDto` | 200 | حالة فعلية مؤكدة | PATH✓ RESP△ |
| GET | `/api/owner/devices/{id}/commands` | M+W | — | `OwnerDeviceCommandResponseDto[]` | 200 | فترة ملكية حالية | PATH✓ RESP△ |
| POST | `/api/owner/devices/{id}/commands` | M+W | `CreateOwnerDeviceCommandRequestDto` | `OwnerDeviceCommandResponseDto` | **202** | `actualStateChanged=false` على القبول | PATH✓ STATUS✓ · schema استجابة + 401/403/409/503 |
| GET | `/api/owner/devices/{id}/commands/{commandId}` | M+W | — | `OwnerDeviceCommandResponseDto` | 200 | | PATH✓ RESP△ |
| GET | `/api/owner/devices/{id}/alarm-events` | M+W | — | `OwnerAlarmEventListItemDto[]` | 200 | `status` ليس `alarmPhase` | PATH✓ RESP△ |
| GET | `/api/owner/devices/{id}/alarm-events/{alarmEventId}` | M+W | — | `OwnerAlarmEventListItemDto` | 200 | يشمل `mutedAt` | PATH✓ RESP△ |
| GET | `/api/owner/notifications` | Mobile | — | `OwnerNotificationListItemDto[]` | 200 | | PATH✓ RESP△ |
| GET | `/api/owner/notifications/{notificationId}` | Mobile | — | `OwnerNotificationDetailDto` | 200 | إعادة تحقق ملكية | PATH✓ RESP△ |

---

## Push tokens — أي مستخدم مصادق

| Method | Route | مستهلك | Request | Response | HTTP | OpenAPI |
|--------|-------|--------|---------|----------|------|---------|
| GET | `/api/me/push-tokens` | Mobile | — | list | 200 | PATH✓ RESP△ |
| POST | `/api/me/push-tokens` | Mobile | `RegisterPushTokenRequestDto` | item | 200 | PATH✓ SCHEMA△ |
| DELETE | `/api/me/push-tokens/{pushTokenId}` | Mobile | — | `{}` | 200 | PATH✓ |
| DELETE | `/api/me/push-tokens` | Mobile | — | `{}` | 200 | PATH✓ |

---

## Admin — Owners (`Users.View` / `Users.Manage`)

| Method | Route | Request | Response | HTTP code | OpenAPI status |
|--------|-------|---------|----------|-----------|----------------|
| GET | `/api/admin/owners` | — | list | 200 | 200 · RESP△ |
| GET | `/api/admin/owners/{id}` | — | item | 200 | 200 · RESP△ |
| POST | `/api/admin/owners` | `CreateOwnerRequestDto` | account | **201** | **201✓** |
| PUT | `/api/admin/owners/{id}` | `UpdateOwnerRequestDto` | account | 200 | 200 · RESP△ |
| POST | `/api/admin/owners/{id}/disable` | — | `{}` | 200 | 200 |
| POST | `/api/admin/owners/{id}/archive` | — | `{}` | 200 | 200 |

مستهلك: `admin_web`.

---

## Admin — Devices (`Devices.View` / `Devices.Manage`)

| Method | Route | Request | Response | HTTP | OpenAPI |
|--------|-------|---------|----------|------|---------|
| GET | `/api/admin/devices` | — | overview[] | 200 | PATH✓ RESP△ |
| GET | `/api/admin/devices/{deviceId}` | — | overview | 200 | PATH✓ RESP△ |
| POST | `/api/admin/devices` | `RegisterDeviceRequestDto` | registered | **201** | **201✓** |
| POST | `/api/admin/devices/{id}/link` | `LinkDeviceRequestDto` | linked | **201** | **201✓** |
| POST | `/api/admin/devices/{id}/transfer` | `TransferDeviceRequestDto` | transferred | 200 | PATH✓ RESP△ |
| POST | `/api/admin/devices/{id}/unlink` | — | unlinked | 200 | PATH✓ RESP△ |
| POST | `/api/admin/devices/{id}/archive` | — | archived | 200 | PATH✓ RESP△ |

---

## Admin — Alerts / faults / audit

| Method | Route | Permission | Request | Response | HTTP | OpenAPI |
|--------|-------|------------|---------|----------|------|---------|
| POST | `/api/admin/alerts` | Manage | `CreateAdminAlertRequestDto` | item | **201** | **201✓** |
| GET | `/api/admin/alerts` | View | query paging | `PagedResultDto` | 200 | PATH✓ RESP△ |
| GET | `/api/admin/alerts/{id}` | View | — | item | 200 | PATH✓ RESP△ |
| GET | `/api/admin/device-faults` | Devices.View | query | `PagedResultDto` | 200 | PATH✓ RESP△ |
| GET | `/api/admin/audit-logs` | AuditLogs.View | query | `PagedResultDto` | 200 | PATH✓ RESP△ |

---

## مصفوفة DTO أوامر (حرج للقبول)

| حقل Backend | JSON | إلزامي عند 202 قبول | ملاحظة |
|-------------|------|---------------------|--------|
| CommandId | commandId | نعم | |
| DeviceId | deviceId | نعم | |
| Type | type | نعم | |
| Status | status | نعم | يبدأ PENDING |
| CommandSequence | commandSequence | نعم | |
| OwnershipVersion | ownershipVersion | نعم | |
| ObservedStateSequence | observedStateSequence | نعم | |
| CreatedAt / ExpiresAt | createdAt / expiresAt | نعم | DateTimeOffset |
| TargetAlarmEventUid | targetAlarmEventUid | لا | مطلوب منطقياً لـ MUTE |
| RejectCode | rejectCode | لا | عند الرفض |
| ResultStateSequence | resultStateSequence | لا | بعد ACK |
| SentAt / AcknowledgedAt / ReconciledAt | … | لا | |
| IdempotentReplay | idempotentReplay | نعم | |
| **ActualStateChanged** | **actualStateChanged** | **نعم = false عند القبول** | لا يعني تنفيذ حماية |
| ConfirmedArmState / ConfirmedSirenState | … | لا | لقطة مؤكدة إن وُجدت |

---

## فجوات OpenAPI المتبقية (حالية — ليست فروقات ما قبل التصحيح)

ما **صُحّح في INT-T-03** ولم يعد فجوة حالية: رموز **201** لأربعة مسارات إنشاء؛ توثيق **202** + مخطط `OwnerDeviceCommandResponseDto` / غلاف الأمر؛ `ApiErrorResponse`؛ `AuthTokensResponseDto` / `MeResponseDto`؛ `security: []` على login/refresh/health.

ما **يتبقى** (موثّق · لا يمنع إغلاق INT-T-03 · لا يعني اكتمال كل مخططات OpenAPI):

1. معظم المسارات ما زالت بلا `content` كامل لاستجابة النجاح (رمز `RESP△`) — PROP-HTTP-03.
2. Cookie refresh + CSRF موثّقان نصياً في المسودة/`error-codes` وليسا `securitySchemes` منفصلين في OpenAPI.
3. مخططات استجابة الأجهزة/الأحداث/الإدارة غير مكتملة في `components.schemas` بخلاف مسار قبول الأمر الحرج.
4. بنود `proposed` فقط في `openapi.proposed.json` (لا تُخلط مع المنفَّذ).
