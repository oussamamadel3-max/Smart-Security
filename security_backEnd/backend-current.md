# المهمة الجارية — باك إند

status: blocked_awaiting_g1
task: T-06 / BE-T-06 الحوادث وطلب الإشعار وEVENT_PERSISTED
srs: FR-021…FR-024 · FR-025 جزئياً — BR-013 BR-014 BR-016 · NFR-005
parent: T-06 / alarm events + notification request + EVENT_PERSISTED
branch: (لم تُنشأ)
worktree: C:/Users/Osama/Desktop/SmartSecurity/security_backEnd

## الحكم الحالي

| البعد | الحالة |
|--------|--------|
| التنفيذ البرمجي الاختباري (البطاقات الثلاث على PostgreSQL + مسودة العقد) | **مكتمل** بأدلة بناء واختبارات |
| مراجعة سباقات Outbox / إعادة EVENT_PERSISTED | **مكتملة** — إصلاح `EnsureEventPersistedOutboxAsync` + اختبارات انحدار |
| تعليم `[x]` / إغلاق المهمة في `backend-development-tasks.md` | **لم يُنفَّذ** — ممنوع قبل `APPROVE TASK CLOSURE: BE-T-06` |
| الإغلاق الإنتاجي لمسارات الحوادث | **BLOCKED** — ينتظر اعتماد `G1` (`INT-T-04`) |
| نشر MQTT / FCM الإنتاجي | **غير مفعّل**: Outbox/Inbound معطّلان · ناشر No-Op |

## المتطلبات السابقة (متحققة)

- `BE-T-04` مكتملة ومغلقة 2026-11-06.
- جداول `alarm_events` / `notifications` / `device_faults` / `event_loss_reports` وقيود التفرد من T-01.
- إعادة استخدام MQTT الوارد + Outbox من T-04/T-05 (بدون ناشر موازٍ).

## تم تنفيذياً (اختباري — مسودة §4.4)

1. **SS-P5-010…030:** كما سبق — حفظ حادثة/إشعار ACTIVE/`EVENT_PERSISTED` عبر Outbox + فقد/أعطال.
2. **مراجعة سباق Outbox:** إعادة فتح صف `EVENT_PERSISTED` بعد `ProcessedAt` تعيد ميزانية المحاولات؛ الصف المعلّق يسرّع `AvailableAt` فقط دون مسح المحاولات؛ `FOR UPDATE` يسلسل مع عامل النشر.

## التحقق (مُنفَّذ — أدلة اختباريّة)

- انظر تقرير مراجعة السباقات في المحادثة الجارية.
- لا `[x]` ولا نسب إنجاز نهائية حتى الموافقة.

## المتبقي لإغلاق إنتاجي

1. محضر اعتماد `G1` لمخطط ALARM_EVENT / EVENT_PERSISTED / العطل / علامة الفقد.
2. محوّل Mosquitto حي + تفعيل مدروس لـ Inbound/Outbox.
3. عامل FCM (T-09) + إلغاء إشعارات النقل (T-07) باختبار عابر.
4. موافقة إغلاق صريحة: `APPROVE TASK CLOSURE: BE-T-06`.

## خارج النطاق الآن

- تعليم `[x]` أو نسب الإنجاز النهائية.
- اعتماد G1 من الباك إند.
- بدء T-07 دون تفويض.
