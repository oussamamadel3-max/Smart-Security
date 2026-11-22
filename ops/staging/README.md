# Staging stack templates — OPS-T-03

**الحالة:** قوالب + تحقق محلي معزول. **ليس** نشراً على VPS ولا Staging Runtime Validated على مضيف حقيقي.

| ملف | الدور |
|-----|--------|
| `docker-compose.staging.yml` | API + PostgreSQL (+ Mosquitto اختياري للمختبر) — منافذ `127.0.0.1` فقط |
| `Caddyfile` | Reverse proxy TLS محلي / قالب Forwarded Headers |
| `Dockerfile.api` | صورة API بلا أسرار مضمّنة |
| `.env.staging.example` | أسماء المتغيرات فقط |
| `SERVICES.md` | قائمة الخدمات المسموح نشرها |
| `ROLLBACK.md` | خطة رجوع وإصدار محدد |
| `HTTPS-ACCEPTANCE.md` | قائمة اختبار HTTPS الحي (تنفَّذ على مضيف حقيقي لاحقاً) |

## سياسة العزل عن Development (OPS-T-01)

- Compose project: `smartsecurity_stg`
- Volumes منفصلة: `smartsecurity_stg_pgdata` (لا تمس `smartsecurity_pgdata`)
- منافذ مضيف مختلفة (مثلاً Postgres `127.0.0.1:5434`)
- قاعدة: `esp32_security_staging`
- MQTT Inbound/LivePublish/Outbox تبقى **false** في القالب

## تشغيل محلي اختياري (مختبر)

```powershell
cd ops/staging
Copy-Item .env.staging.example .env.staging   # عدّل كلمات مرور اختبار فقط
docker compose --env-file .env.staging -f docker-compose.staging.yml config
# لا تستخدم down -v على بيانات موجودة. لا تنشر خارج 127.0.0.1.
```

البناء الكامل للصورة قد يستغرق وقتاً؛ التحقق الإلزامي الأدنى: `config` + سكربتات `Test-OpsT03*.ps1`.

## ما لا يدّعيه هذا المجلد

- Staging Runtime Validated على نطاق/شهادة عامة
- اجتياز G1 أو تفعيل MQTT إنتاجي
- نشر عمال Outbox/FCM (غير مسموح حتى جاهزية Backend)
