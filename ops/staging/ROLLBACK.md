# خطة نشر محدد ورجوع — OPS-05 (OPS-T-03)

## تعريف الإصدار

سجّل قبل أي ترقية (حتى المختبر المحلي):

| حقل | مصدر |
|-----|------|
| `DEPLOY_IMAGE_TAG` | وسم صورة API |
| `DEPLOY_GIT_SHA` | `git rev-parse HEAD` (يُحقن كمتغير بيئة؛ ليس سراً) |
| Compose file | `docker-compose.staging.yml` |
| Env file | `.env.staging` (خارج Git) |

## ترتيب النشر الآمن (مختبر / لاحق staging حقيقي)

1. نسخ احتياطي لقاعدة staging (OPS-T-04 يغطّي الجدولة؛ يدوياً قبل ترحيل: `pg_dump`).
2. مراجعة Migrations المراد تطبيقها — **لا** migrate تلقائي مدمّر في صورة الدخول دون موافقة.
3. سحب/بناء الصورة بالوسم المحدد.
4. `docker compose up -d` مع healthchecks.
5. تحقق: `/health/live` = 200، `/health/ready` = 200.
6. تحقق يدوي login (بدون لصق tokens في السجلات).

## الرجوع

1. أعد تشغيل الحاوية بالوسم السابق (`DEPLOY_IMAGE_TAG` السابق).
2. إن فشل ترحيل مخطط: استعد من `pg_dump` على قاعدة staging فقط — **لا** تلمس Development volumes.
3. لا تستخدم `docker compose down -v` إلا بموافقة صريحة (يمحو البيانات).

## ما لا يُرجَع عبر صورة API

- أسرار مدير الأسرار / `.env.staging`
- شهادات TLS العامة (منفصلة عن OPS-T-01 test CA)
