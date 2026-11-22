# دليل تشغيل Staging — OPS-T-03

## المتطلبات المسبقة

- OPS-T-01 Compose محلي سليم (لا تخلط volumes)
- OPS-T-02 قوالب أسرار/CORS
- Docker Compose v2
- ملف `ops/staging/.env.staging` من المثال (gitignored)

## إعداد الشبكات والشهادات

- كل `ports:` في `docker-compose.staging.yml` على `127.0.0.1`
- شهادات المختبر: Caddy `tls internal` أو شهادات اختبار — **ممنوع** على إنتاج
- مضيف حقيقي لاحقاً: شهادة بيئة من مدير أسرار؛ لا تنسخ `ops/tls` الاختبارية

## الأسرار

- `Auth__Jwt__SigningKey` و`POSTGRES_PASSWORD` من `.env.staging` فقط
- لا تُمرَّر كـ build-arg إلى Dockerfile
- راجع `../scripts/Test-SecretsHygiene.ps1`

## تشغيل المختبر المحلي

```powershell
cd ops/staging
Copy-Item .env.staging.example .env.staging
# عدّل كلمات المرور ومفتاح JWT (≥32) في الملف gitignored فقط
docker compose --env-file .env.staging -f docker-compose.staging.yml config
powershell -File ..\scripts\Test-StagingLocalRuntime.ps1
# يبني الصورة، يفحص /health عبر HTTPS محلي، يعيد التشغيل، ثم down بدون -v
```

نجاح هذا السكربت = **Local Runtime Validated** فقط. ليس **Staging Runtime Validated** ولا يغني عن H1–H7 على مضيف عام.

## Forwarded Headers

- الافتراضي في API: ثقة loopback فقط (`ForwardedHeadersSetup`).
- مكدس المختبر يفعّل `ForwardedHeaders__TrustPrivateNetworkProxies=true` لأن Kestrel غير منشور على المضيف.
- Caddy يحذف ترويسات `X-Forwarded-*` القادمة من العميل ثم يضبطها من الاتصال الحقيقي.

## فحوص الصحة

| مسار | المعنى | فشل يعني |
|------|--------|----------|
| `GET /health/live` | العملية حية | أعد تشغيل API فقط |
| `GET /health/ready` | PostgreSQL قابل للاتصال | لا تعيد تشغيل API في حلقة بسبب DB — أصلح DB أولاً |
| `GET /api/health` | غلاف JSON قديم (liveness دلالي) | للتوافق؛ لا تستخدمه readiness |

## MQTT

- افتراضياً **معطّل** على API
- بروفايل `mqtt` يشغّل وسيطاً للمختبر فقط
- ACL: أعد استخدام مصفوفة OPS-T-01 على منافذ staging المحلية إن لزم

## تشخيص

```powershell
docker compose --env-file .env.staging -f docker-compose.staging.yml ps
docker compose --env-file .env.staging -f docker-compose.staging.yml logs --tail=100 api
# لا تلصق Authorization أو refresh في التقارير
```

## إيقاف آمن

```powershell
docker compose --env-file .env.staging -f docker-compose.staging.yml stop
# أو down بدون -v للإبقاء على smartsecurity_stg_pgdata
docker compose --env-file .env.staging -f docker-compose.staging.yml down
```

## استئناف التحقق عند توفر مضيف staging حقيقي

1. انقل القوالب إلى مضيّف معزول (ليس Production).
2. ثبّت شهادة TLS حقيقية.
3. نفّذ `HTTPS-ACCEPTANCE.md` H1–H7.
4. حدّث DEP-WEB-01 / G1-evidence بالنتائج.
