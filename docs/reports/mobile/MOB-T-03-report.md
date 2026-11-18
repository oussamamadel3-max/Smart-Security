# تقرير المهمة MOB-T-03 — قائمة الأجهزة وحالة الربط

| الحقل | القيمة |
|--------|--------|
| رقم المهمة | MOB-T-03 / T-03 |
| الاسم | قائمة الأجهزة وحالة الربط |
| الحالة | **CLOSED** — إغلاق تلقائي بموجب استثناء Autonomous Sequential Mobile (2026-11-17) |
| التاريخ | 2026-11-17 |
| المشروع | `security_mobile` |

## 1. الملخص التنفيذي

نُفّذت قائمة أجهزة المالك عبر `GET /api/owner/devices` مع بطاقات مؤشرات منفصلة، عرض PENDING المحدود، وACTIVE مع انتظار مزامنة الملكية، ومسح cache عند فقد الجلسة/الصلاحية. التفاصيل للقراءة فقط دون أوامر (نطاق T-04).

## 2–4. المتطلبات والبطاقات

- [x] قائمة الأجهزة + تفريغ فارغ/خطأ شبكة
- [x] بطاقة مؤشرات اتصال/حماية/صفارة/عتاد
- [x] عرض PENDING المحدود مقابل ACTIVE
- [x] التفريق بين الربط الأولي ونقل الملكية (awaitingOwnershipAck / canControl)

## 5–7. الملفات

أضيفت طبقات `features/devices/**`، وتحديثات router/`app_endpoints`/l10n. لا حذف جوهري.

## 8–9. التنفيذ والقرارات

- مصدر العقد: `OwnerDeviceListItemDto` / `OwnerDevicesController`.
- PENDING بدون indicators؛ ACTIVE يعرض المؤشرات الأربعة منفصلة.
- `clearCache` عند logout / تبديل حساب / 401/403/404 على التفصيل.

## 10–13. الاختبارات والبناء

| الأمر | النتيجة |
|--------|---------|
| `flutter analyze` | نظيف |
| `flutter test` | **20/20** |
| APK `dev`/`android-arm64` | `app-dev-debug.apk` ✓ |

جهاز/محاكٍ وiOS: **NOT VERIFIED** (قرار المالك).

## 14. قبول

| الشرط | النتيجة |
|--------|---------|
| لا خلط offline مع disarm/health | PASSED |
| PENDING محدود | PASSED |
| ACTIVE + awaiting sync | PASSED |
| مسح cache عند فقد التفويض | PASSED |
| جهاز أجنبي لا يظهر | PASSED (عزل الخادم + مسح محلي عند 403/404) |
| ميداني | NOT VERIFIED |

## 15–17. نسب ومهمة تالية

الموبايل → **3/8 (37.5%)** بعد الإغلاق. التالية: **MOB-T-04**.

## الدرس الهندسي

المؤشرات الأربعة يجب أن تُعرض كحقول مستقلة؛ `ConnectionStatus.offline` لا يغيّر `ArmStatus` ولا يفترض `HEALTHY`. مرجع: `DeviceIndicatorsEntity` + `OwnerDeviceQueryService.MapIndicators`.
