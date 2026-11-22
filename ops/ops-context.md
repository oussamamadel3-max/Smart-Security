# سياق المشروع — التشغيل والبيئات (`ops`)

آخر تحديث: 2026-11-22 (تنفيذ OPS-T-03 بانتظار إغلاق؛ 2/6 ثابت؛ النظام 26/50؛ G1 BLOCKED)  
السياق العام: [`../system-context.md`](../system-context.md) · التقدّم: [`ops-progress.md`](./ops-progress.md)

## الهوية

| الحقل | القيمة |
|--------|--------|
| المسؤولية | Compose محلي، عزل بيئات، نشر/صحة، نسخ، مراقبة، ميداني |
| التقنية | Docker Compose · PostgreSQL 16 · Mosquitto · Caddy قالب · probes live/ready |
| مسار العمل | `ops/` — T-01/T-02 مغلقتان؛ T-03 برمجي مكتمل بتحقق خارجي معلّق |
| لا يملك | منطق أعمال API · firmware · عقود G1 |

## مبادئ ملزمة

- لا أسرار في Git/صور.
- قوالب staging ≠ نشر VPS ≠ Staging Runtime Validated.
- MQTT/Outbox/FCM إنتاجي DISABLED.
- `/health/live` ≠ `/health/ready` ≠ اكتمال سلسلة الأمر.

## المهمة الجارية

- **OPS-T-03** — EXTERNAL VALIDATION PENDING (HTTPS حي).
- التفاصيل: [`ops-current.md`](./ops-current.md)

## تتبّع المهام

| مهمة | الحالة |
|------|--------|
| T-01 | ✓ مغلقة |
| T-02 | ✓ مغلقة |
| T-03 | تنفيذ برمجي — بانتظار موافقة الإغلاق |
| T-04…T-06 | معلّقة |

**ملخص:** 2/6 رسمي · T-03 بانتظار الإغلاق
