# OPS-T-03 — تقرير نشر Staging والصحة والتحقق

| الحقل | القيمة |
|--------|--------|
| المهمة | `OPS-T-03` / `T-03` |
| التاريخ | 2026-11-22 |
| الحكم | **IMPLEMENTATION COMPLETE — EXTERNAL VALIDATION PENDING** |
| إغلاق رسمي | **لم يُغلق** — ينتظر `APPROVE TASK CLOSURE: OPS-T-03` |
| العدّاد الرسمي | **قبل = بعد = 26/50** · Ops **2/6** |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED` |

---

## 1. نص المهمة الرسمية ومتطلباتها

من `ops/ops-development-tasks.md` — **T-03 نشر الخدمات والصحة**:

- نشر API + PostgreSQL (+ MQTT عند الحاجة) بحدود شبكة؛ **لا** عمال MQTT/Outbox/FCM غير الجاهزة.
- HTTPS + Reverse Proxy + Forwarded Headers + TLS.
- اختبارات staging HTTPS حقيقية لـ Cookie/CSRF/CORS.
- فصل liveness/readiness؛ restart؛ إصدار محدد؛ ترحيلات بمراجعة وخطة رجوع.
- **القبول:** PG غير عام؛ الوسيط يرفض المجهول/الأجنبي؛ استعادة الخدمات المنفذة بعد restart؛ نتائج HTTPS موثّقة؛ live/ready مميّزان؛ health ≠ اكتمال السلسلة.

**البطاقات:** OPS-02 · OPS-04 · OPS-05 · HTTPS حي · probes · قائمة خدمات مقيّدة.

---

## 2. حالة المشروع عند البدء

| بند | قيمة |
|-----|------|
| Official | **26/50 (52%)** |
| Ops | **2/6** (T-01، T-02 مغلقتان) |
| OPS-T-02 CLOSURE | ✓ موجود |
| INT / BE / ESP / Mobile / Web | 3/6 · 5/13 · 0/9 · 8/8 · 8/8 |

---

## 3–4. الاعتماديات

| اعتماد | الحالة |
|--------|--------|
| OPS-T-02 عزل أسرار/بيئات | ✓ |
| OPS-T-01 Compose محلي | ✓ healthy |
| بناء API قابل للنشر | ✓ (Dockerfile قالب) |
| مضيف Staging عام + شهادة | **غير متوفر** — DEP-OPS-03 |
| G1 لتفعيل MQTT إنتاجي | BLOCKED — يبقى DISABLED |

---

## 5. مخرجات T-01/T-02 المعاد استخدامها

- Compose dev + ACL/TLS محلي + volumes منفصلة عن staging.
- قوالب `environments/staging` + سياسات CORS/Cookie.
- `Test-SecretsHygiene` كانحدار.

---

## 6–8. الأعمال المنفَّذة والملفات

### Backend (ضمن نطاق probes/proxy للمهمة)

- `API/Program.cs`: `/health/live` · `/health/ready` (EF DbContext check) · alias `/health` = live · `UseForwardedHeaders`
- `HealthController`: توثيق أن `/api/health` إرث JSON
- حزمة `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `API.IntegrationTests/HealthProbeTests.cs`

### Ops staging

- `ops/staging/docker-compose.staging.yml` (مشروع/volumes منفصلة، `127.0.0.1`، MQTT API false)
- `Dockerfile.api` · `Caddyfile` · `.env.staging.example`
- `SERVICES.md` · `ROLLBACK.md` · `HTTPS-ACCEPTANCE.md` · `README.md`
- `ops/docs/STAGING-RUNBOOK.md`
- سكربتات: `Test-StagingComposeConfig` · `Test-NetworkExposure` · `Test-HttpsSecurityChecklist` · `Test-OpsT03`

---

## 9–10. HTTPS/TLS وPostgreSQL/MQTT

| عنصر | الحالة |
|------|--------|
| Caddy TLS + X-Forwarded-Proto | Configuration Validated |
| شهادات اختبار على مضيف عام | ممنوع / غير منفَّذ |
| Postgres staging ports | `127.0.0.1:5434` قالب |
| MQTT API | hard-disabled |
| Mosquitto profile | اختياري للمختبر فقط |

---

## 11–12. نتائج الأمان والتشغيل

| اختبار | النتيجة | مستوى الدليل |
|--------|---------|----------------|
| `Test-OpsT03.ps1` (config) | **PASS** | Configuration Validated |
| `Test-StagingLocalRuntime.ps1` | **PASS** | **Local Runtime Validated** |
| Staging `compose config` | **PASS** | Configuration Validated |
| Network exposure loopback | **PASS** | Configuration + runtime T-01 |
| HTTPS checklist (قوالب) | **PASS** | Configuration Validated |
| Secrets hygiene | **PASS** | Configuration Validated |
| HealthProbeTests + ForwardedHeadersSetupTests | **6/6 PASS** | Local Runtime (test host) |
| بناء صورة `smartsecurity-api:local-stg` | **PASS** | Local Runtime |
| `/health/live` و`/health/ready` عبر Caddy `local_certs` | **HTTP 200** | Local Runtime — ليس HTTPS عام |
| إعادة التشغيل + بقاء `smartsecurity_stg_pgdata` بعد `down` بلا `-v` | **PASS** | Local Runtime |
| HTTPS H1–H7 على مضيف عام | **NOT RUN** | Staging Runtime Validated — ناقص |

التفاصيل: `docs/reports/ops/OPS-T-03-supplement-report.md`.

### مراجعة أمنية مختصرة

| اكتشاف | شدة | معالجة |
|--------|-----|--------|
| غياب مضيف staging يمنع إثبات Cookie Secure العام | High (للقبول التشغيلي) | DEP-OPS-03 — لا تخفيف معيار |
| تفريغ KnownProxies كان سيثق بأي عميل | High | عُولج: `ForwardedHeadersSetup` + API بلا منفذ مضيف |
| مفاتيح DataProtection داخل الحاوية غير مشفّرة وغير دائمة | Low | تحذير وقت التشغيل؛ خارج إغلاق T-03 |

---

## 13. اختبارات Staging غير المنفذة

| اختبار | السبب |
|--------|--------|
| H1–H7 HTTPS عام | لا نطاق/شهادة/مضيف (DEP-OPS-03) |
| متصفح CSRF/CORS حي | DEP-WEB-01 |
| restart كامل لمكدس staging API | **نُفّذ محلياً** في التحقق التكميلي — انظر التقرير التكميلي |

---

## 14–15. فجوات

| فجوة | حالة |
|------|------|
| DEP-OPS-03 مضيف staging | جديد — خارجي |
| DEP-WEB-01 HTTPS حي | معلّق |
| JWT multi-key | خارج النطاق (سابق) |
| عمال Outbox/FCM | لا تُنشر — صحيح |

---

## 16. مصفوفة شروط القبول

| شرط | التصنيف |
|-----|---------|
| PostgreSQL غير عام (قالب + T-01) | **VERIFIED** (loopback) |
| وسيط يرفض المجهول/الأجنبي | **VERIFIED** (انحدار T-01؛ قالب mqtt اختياري) |
| استعادة بعد restart للخدمات المنفذة | **VERIFIED** محلياً (restart + volume). ليس على مضيف عام |
| نتائج HTTPS/Cookie/CSRF/CORS موثّقة على staging حقيقي | **NOT VERIFIED** |
| تجهيز قائمة HTTPS + قوالب آمنة | **VERIFIED** |
| liveness/readiness مميّزان | **VERIFIED** (كود + 3 اختبارات) |
| قائمة خدمات مقيّدة | **VERIFIED** (`SERVICES.md`) |
| لا عمال MQTT/Outbox/FCM | **VERIFIED** |

→ **لا READY TO CLOSE** بينما شرط HTTPS الحي في نص القبول غير متحقق.

---

## 17. أثر البوابات

لا إغلاق G0/G1/G2 · MQTT Production DISABLED · لا PRODUCTION READY.

---

## 18. العدادات

| | قبل | بعد التنفيذ |
|--|-----|-------------|
| النظام | 26/50 | **26/50** |
| Ops | 2/6 | **2/6** |

---

## 19. جاهزية الإغلاق

**IMPLEMENTATION COMPLETE — EXTERNAL VALIDATION PENDING**

**SOFTWARE COMPLETE:** القوالب، Forwarded Headers، صورة API، الصحة، إعادة التشغيل، وHTTPS المختبر المحلي.

**STAGING VALIDATED:** لا. H1–H7 على مضيف عام ما زالت شرطاً غير متحقق، لذلك لا READY TO CLOSE ولا إغلاق ذاتي.

---

## 20. التالي

| مهمة | ملاحظة |
|------|--------|
| OPS-T-04 | تعتمد على staging على الأقل — بعد إغلاق T-03 |
| INT-T-04 | START منفصل |
| استئناف HTTPS | DEP-OPS-03 + DEP-WEB-01 |

لا بدء تلقائي.

---

## 21. درس هندسي

**المفهوم:** فصل `/health/live` عن `/health/ready` يمنع حلقة إعادة تشغيل API عند سقوط PostgreSQL.

**لماذا ينجح:** منصات الحاويات تعيد تشغيل الحاوية عند فشل liveness؛ جاهزية DB يجب أن تفشل readiness فقط.

**متى:** أي نشر Compose/K8s خلف proxy.

**تحقق:** `HealthProbeTests` + إيقاف DB مؤقتاً يترك `/health/live=200` و`/health/ready≠200` (على بيئة اختبار معزولة).

---

## انتظار المالك

```text
APPROVE TASK CLOSURE: OPS-T-03
```

(مع إدراك أن HTTPS الحي ما زال معلّقاً ما لم يُعدّل شرط القبول أو يُوفَّر مضيف.)
