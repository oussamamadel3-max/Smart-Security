# Operations — التطوير المحلي وعزل البيئات

مكدس Docker Compose معزول لمشروع **نظام الأمان الذكي**: PostgreSQL + Eclipse Mosquitto (ACL + TLS اختبار محلي) — **OPS-T-01**.  
عزل البيئات والأسرار (قوالب + سياسات + تحقق نظافة) — **OPS-T-02** (انظر أدناه).

| عنصر | قيمة |
|------|------|
| Compose project | `smartsecurity` |
| Network | `smartsecurity_local` |
| PostgreSQL image | `postgres:16-alpine` |
| Mosquitto image | `eclipse-mosquitto:2` (broker **2.1.2**) |
| Postgres host port | `127.0.0.1:5433` → 5432 (يتجنب تعارض المضيف على 5432) |
| MQTT plaintext | `127.0.0.1:1883` |
| MQTT TLS | `127.0.0.1:8883` |
| Volumes | `smartsecurity_pgdata`, `smartsecurity_mosquitto_data` |

**سياسة المالك:** `ISOLATED LOCAL INTEGRATION ALLOWED — PRODUCTION DISABLED`.  
نجاح هذا المكدس لا يغلق `G0`/`G1`/`G2` ولا يفعّل MQTT/Outbox الإنتاجي في Backend.

## المتطلبات المسبقة

- Docker Desktop / Docker Engine + Compose v2
- PowerShell 7+ أو Windows PowerShell 5.1
- لا حاجة لـ OpenSSL على المضيف (التوليد عبر حاوية `alpine/openssl`)

## الإعداد السريع

```powershell
cd ops
Copy-Item .env.example .env   # إن لم يُنشأ تلقائياً
# عدّل كلمات المرور في .env (قيم تجريبية فقط)
.\scripts\Bootstrap-LocalStack.ps1
docker compose --env-file .env up -d
.\scripts\Test-LocalStack.ps1
.\scripts\Test-LocalTls.ps1
```

`Bootstrap-LocalStack.ps1` ينشئ:

1. `.env` من المثال إن لزم
2. `mosquitto/passwd` (مشفّر، gitignored) للمستخدمين `srv` / `deva` / `devb`
3. شهادات اختبار في `tls/server/` (gitignored)

## ربط Backend المحلي (اختياري)

API الافتراضي يستخدم `localhost:5432`. للتطوير ضد حاوية OPS:

- Connection string مقترح (User Secrets / env — **لا** تُرفع أسرار):  
  `Host=localhost;Port=5433;Database=esp32_security_dev;Username=ss_dev;Password=<from .env>`
- أبقِ في `appsettings.json`:  
  `Mqtt:Inbound:Enabled=false`, `Mqtt:Outbound:LivePublishEnabled=false`, `Mqtt:Outbox:Enabled=false`, `UseTls=false`
- لا تُشغَّل Migrations مدمِّرة ضد بيانات مشتركة. طبّق EF على قاعدة التطوير المعزولة فقط عند الحاجة بموافقة منفصلة.

## التحقق اليدوي السريع

```powershell
docker compose --env-file .env ps
docker inspect --format "{{.State.Health.Status}}" smartsecurity-postgres
docker inspect --format "{{.State.Health.Status}}" smartsecurity-mosquitto
docker compose --env-file .env logs --tail=50 mosquitto
```

## اختبارات ACL

الهويات التجريبية:

| مستخدم | الدور | مسموح |
|--------|-------|--------|
| `srv` | خادم اختبار | `readwrite devices/#` |
| `deva` | جهاز A | write state/events/acks لجهاز A؛ read commands لجهاز A |
| `devb` | جهاز B | نفس النمط لجهاز B |

معرّفات الأجهزة الثابتة في `.env.example` متوافقة مع مسودة MQTT v1 و`API.IntegrationTests/mosquitto/acl`.

```powershell
.\scripts\Test-AclMatrix.ps1
```

المصفوفة تثبت: رفض المجهول، سماح قنوات الذات، رفض عبور الأجهزة، رفض نشر الجهاز على `commands`، رفض كلمة مرور خاطئة.

## السجلات والإيقاف

```powershell
docker compose --env-file .env logs -f
docker compose --env-file .env stop          # إيقاف مع الإبقاء على الحاويات
docker compose --env-file .env down          # إيقاف + إزالة حاويات — **يُبقي volumes**
docker compose --env-file .env up -d         # إعادة تشغيل دون فقدان بيانات الـvolume
```

حذف البيانات المحلية **عمداً فقط**:

```powershell
docker compose --env-file .env down -v
```

## مشكلات شائعة

| عرض | تشخيص |
|-----|--------|
| `docker: pipe dockerDesktopLinuxEngine` | شغّل Docker Desktop ثم أعد المحاولة |
| تعارض منفذ 5432 | المكدس يستخدم **5433** عمداً |
| Mosquitto unhealthy | تأكد من وجود `mosquitto/passwd` و`tls/server/*.crt` عبر Bootstrap |
| ACL allow بالخطأ | لا تستخدم وسيطاً anonymously؛ راجع `mosquitto/acl` |
| Backend لا يتصل | تحقق من المنفذ 5433 وUser Secrets وليس `appsettings` الإنتاجي |

## OPS-T-02 — عزل البيئات والأسرار

| وثيقة / أداة | المسار |
|--------------|--------|
| قوالب البيئات | [`environments/`](./environments/) |
| عزل البيئات | [`docs/ENV-ISOLATION.md`](./docs/ENV-ISOLATION.md) |
| أسرار وJWT | [`docs/SECRETS-AND-JWT.md`](./docs/SECRETS-AND-JWT.md) |
| قائمة تحقق الأسرار | [`docs/SECRETS-CHECKLIST.md`](./docs/SECRETS-CHECKLIST.md) |
| CORS/Cookie/CSRF | [`docs/WEB-SECURITY-PER-ENV.md`](./docs/WEB-SECURITY-PER-ENV.md) |
| جدار ناري | [`docs/FIREWALL-AND-NETWORK.md`](./docs/FIREWALL-AND-NETWORK.md) |

```powershell
cd ops
.\scripts\Test-OpsT02.ps1
# أو أجزاء منفصلة:
.\scripts\Test-SecretsHygiene.ps1
.\scripts\Test-EnvIsolationDocs.ps1
```

- Staging/Production في هذه المهمة **قوالب فقط** — لا نشر حي ولا تفعيل MQTT إنتاجي.
- اختبار HTTPS الحقيقي على مضيف staging مؤجّل لـ **OPS-T-03** (القائمة مُجهَّزة في `web-origins.example.json`).

## OPS-T-03 — قوالب Staging والنشر والصحة

| عنصر | مسار |
|------|------|
| Compose staging-like | [`staging/docker-compose.staging.yml`](./staging/docker-compose.staging.yml) |
| دليل التشغيل | [`docs/STAGING-RUNBOOK.md`](./docs/STAGING-RUNBOOK.md) |
| تحقق | `.\scripts\Test-OpsT03.ps1` |

```powershell
cd ops
.\scripts\Test-OpsT03.ps1
```

- منافذ `127.0.0.1` فقط · MQTT/Outbox/FCM معطّلة في القالب.
- HTTPS على مضيف staging عام = **NOT VERIFIED** حتى يتوفر المضيف.

## خارج النطاق

- OPS-T-04…T-06، INT-T-04، تفعيل MQTT إنتاجي، نشر VPS بلا تفويض، عتاد ESP32، تشغيل Flutter على جهاز.
