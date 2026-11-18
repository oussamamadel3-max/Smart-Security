# تقرير المهمة MOB-T-06 — إشعارات FCM (عميل)

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | MOB-T-06 / T-06 |
| الحالة | **CLOSED** (نطاق العميل) — إغلاق تلقائي Autonomous Sequential |
| التاريخ | 2026-11-17 |

## الملخص

طُبّقت دورة حياة رمز الدفع عبر `/api/me/push-tokens` (تسجيل/إبطال عند الخروج)، ومنفذ `FcmMessagingPort` مع `NoOpFcmMessaging` حتى توفر ملفات Firebase، وفتح الإشعار عبر `notificationId` → `GET /api/owner/notifications/{id}` دون تخزين تفاصيل محلية عند 403/404.

## تحقق

| الأمر | النتيجة |
|--------|---------|
| analyze | نظيف |
| test | **27/27** |
| APK | `app-dev-debug.apk` ✓ |
| Firebase/APNs حي / جهاز | **NOT VERIFIED** |

## قبول

| البند | الحالة |
|--------|--------|
| FCM-03 تسجيل/إبطال HTTP + lifecycle | VERIFIED (مع NoOp حتى Firebase) |
| FCM-05 فتح → API | VERIFIED (وحدة) |
| 403/404 بلا تفاصيل محلية | VERIFIED |
| FCM-01 Firebase/APNs | BLOCKED مؤجّل |
| FCM-06 ميداني | NOT VERIFIED |

## التالية

بدء **MOB-T-07**.
