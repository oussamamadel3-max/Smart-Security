# محضر مراجعة توافق HTTP / OpenAPI — INT-T-03

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-22 |
| المهمة | `INT-T-03` |
| نوع المراجعة | تقنية مستقلة (AI) — **ليست** توقيعاً بشرياً نهائياً |
| نطاق | عقود HTTP المنفَّذة + مسودة OpenAPI + استهلاك Flutter |
| G1 | يبقى **BLOCKED** — هذه المراجعة لا تغلق البوابة |

## 1. Backend Review

| سؤال | الحكم | الدليل |
|------|--------|--------|
| هل المسارات في OpenAPI تطابق Controllers؟ | **COMPATIBLE** | 37 عملية / 30 path — جرد `HTTP-ENDPOINT-INVENTORY.md` |
| هل POST أوامر يعيد 202 مع `actualStateChanged=false`؟ | **COMPATIBLE** | `OwnerDeviceCommandsController` + `OwnerDeviceCommandResponseDto` |
| هل OpenAPI يوثّق الاستجابات كاملة؟ | **PARTIAL** | أُضيفت مخططات أمر/هوية/خطأ؛ بقية الاستجابات بلا `content` كامل |
| هل رموز 201 موثَّقة؟ | **COMPATIBLE بعد INT-T-03** | overlay صحّح 4 مسارات كانت 200 |
| Cookie vs body refresh؟ | **COMPATIBLE** | `LoginRequestDto.RefreshDelivery` + تنفيذ Auth |
| هل OpenAPI يشرعن مخالفة معتمدة؟ | **لا** | الفجوات مؤجّلة صراحة؛ MQTT إنتاج معطّل |

**حكم Backend:** `APPROVE_WITH_GAPS` (توثيق) — الفجوات لا تمنع مسودة INT-T-03؛ تمنع ادّعاء اكتمال مخططات كل DTO وG1.

## 2. Mobile Review

| سؤال | الحكم | الدليل |
|------|--------|--------|
| `refreshDelivery=body`؟ | **COMPATIBLE** | `security_mobile/.../auth_remote_datasource.dart` |
| مسارات أجهزة/أوامر/أحداث؟ | **COMPATIBLE** | `AppEndpoints` |
| DTO أمر ↔ Backend؟ | **COMPATIBLE** | `device_command_dto.dart` يشمل `actualStateChanged` |
| 202 ≠ تنفيذ حماية؟ | **COMPATIBLE** | `device_detail_view_model` + `CommandStatus` في core |
| `DEVICE_FAULT` تفصيلي؟ | **PARTIAL** | يُعرض جزئياً عبر إشعارات/مؤشرات؛ ليس مسار HTTP منفصل |
| `alarmPhase` على HTTP؟ | **N/A** | HTTP يستخدم `status`؛ متوافق مع Backend |

**حكم Mobile:** `COMPATIBLE` لنطاق العميل الحالي مع فجوات غير مانعة لـ INT-T-03.

## 3. Web Review

| سؤال | الحكم | الدليل |
|------|--------|--------|
| `refreshDelivery=cookie` + CSRF؟ | **COMPATIBLE** | `admin_web` auth datasource/constants |
| أوامر مالك + إدارة؟ | **COMPATIBLE** | مسارات owner + admin في التطبيق |
| 202 ≠ تنفيذ؟ | **COMPATIBLE** | نفس نموذج الأمر + UI `actualStateChanged` |
| تشغيل متصفح حي ضد Backend؟ | **NOT VERIFIED** | خارج قيود INT-T-03 / مؤجّل WEB-EXT |

**حكم Web:** `COMPATIBLE` برمجياً؛ التحقق الحي CSRF/CORS ضد بيئة مشتركة **NOT VERIFIED**.

## 4. smart_security_app_core

يوفر `DefaultAuthPaths` ورموز الحالة (`CommandStatus.pending` وتعليق HTTP 202). لا يملك DTOs أجهزة — مقصود. **COMPATIBLE by design.**

## 5. مصفوفة توافق مختصرة

| مجال | Backend | Mobile | Web | OpenAPI draft |
|------|---------|--------|-----|---------------|
| Auth channels | ✓ | ✓ body | ✓ cookie | ✓ موثّق |
| Commands 202/PENDING | ✓ | ✓ | ✓ | ✓ 202 + schema |
| Ownership PENDING≠ACTIVE | ✓ | ✓ | ✓ | موثّق في المسودة |
| Alarms status/mutedAt | ✓ | ✓ | ✓ | عيّنة |
| Full response schemas | △ | — | — | PARTIAL |
| G1 production | ✗ | ✗ | ✗ | ✗ |

## 6. فجوات مصنّفة (لا تُغلق هنا)

| فجوة | تصنيف INT-T-03 |
|------|----------------|
| S1 / DEP-SAFE-01 استرداد حماية بعد تلف إعداد | غير مانعة — مؤجّلة عتاد/قرار |
| DEP-FLUT-01/02 · Mobile R-12/R-13 · WEB-EXT-10/11 | غير مانعة — عميل/خارجي |
| MutedAt كتابة / BE-T-05 | غير مانعة لعقد القراءة؛ إحالة Backend |
| DEVICE_FAULT مسار مالك منفصل | `proposed` PROP-HTTP-02 |
| OWNERSHIP_SYNC Outbox إنتاج | خارجي مؤجّل — MQTT DISABLED |
| healthCode استهلاك كامل | غير مانعة |
| اعتماد عتادي D-01 | خارجي مؤجّل — لا يمنع INT-T-03 |
| مراجعة بشرية ثلاثية رسمية | معلّقة حتى موافقة مالك على الإغلاق / لاحق G1 |

## 7. التوقيعات

| دور | الحالة |
|-----|--------|
| مراجعة تقنية AI — Backend | مكتملة (هذا المحضر) |
| مراجعة تقنية AI — Mobile | مكتملة |
| مراجعة تقنية AI — Web | مكتملة |
| اعتماد مالك بشري للمهمة | **مقبول** — `APPROVE TASK CLOSURE: INT-T-03` (2026-11-22) ضمن نطاق المسودة |
| اعتماد G1 | **غير مطلوب هنا** — يبقى لـ INT-T-04 |

## 8. الحكم التجميعي للمراجعة

`APPROVE_WITH_GAPS` ثم إغلاق مالك — المسودة معتمدة كعقد HTTP مشترك بين Backend وFlutter ضمن نطاق INT-T-03، مع بقاء G1 مغلقة وMQTT الإنتاجي معطّلاً. لا توقيعات بشرية مخترعة لممثلي المشاريع؛ اعتماد المالك يغطي قبول المهمة وفق بديل المحضر التقني + الموافقة الصريحة.
