# OPS-T-03 — تقرير تكميلي: قبول برمجي والتحقق المتبقي

| الحقل | القيمة |
|--------|--------|
| التاريخ | 2026-11-22 |
| الحكم | **IMPLEMENTATION COMPLETE — EXTERNAL VALIDATION PENDING** |
| إغلاق رسمي | **لم يُغلق** — لا `[x]` ولا تغيير عدّاد |
| العدّاد | **26/50** · Ops **2/6** (قبل = بعد) |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED` |

هذا التقرير يكمّل `OPS-T-03-report.md`. المالك اعتمد مبدئياً الجزء البرمجي ولم يوافق على إغلاق المهمة ولا على تعديل شرط HTTPS الحي.

---

## 1. فصل المستويين

| المستوى | المعنى | الحالة |
|---------|--------|--------|
| **SOFTWARE COMPLETE** | قوالب، كود الصحة، Forwarded Headers، سكربتات، صورة API، تشغيل مختبر محلي | **نعم** |
| **Local Runtime Validated** | المكدس اشتغل على Docker المحلي بشهادة `local_certs` | **نعم** |
| **STAGING VALIDATED** | HTTPS عام: Cookie / CSRF / CORS / Origin على مضيف Staging حقيقي (H1–H7) | **لا** |
| Production Validated | — | **لا** · MQTT/Outbox إنتاجيان معطّلان |

---

## 2. Forwarded Headers

المشكلة السابقة: تفريغ `KnownProxies` و`KnownIPNetworks` يجعل ASP.NET يثق بأي عميل يرسل `X-Forwarded-*`.

المعالجة في `API/Configuration/ForwardedHeadersSetup.cs`:

- بلا إعداد: تبقى قوائم ASP.NET الافتراضية (loopback) و`ForwardLimit = 1`.
- الثقة الموسعة فقط عند `KnownProxies` أو `KnownNetworks` أو `TrustPrivateNetworkProxies`.
- مكدس المختبر يفعّل الشبكات الخاصة لأن الحاوية لا تنشر منفذ API على المضيف.
- Caddy يحذف ترويسات العميل قبل التوجيه.

اختبارات: `ForwardedHeadersSetupTests` **3/3 PASS**.

---

## 3. تشغيل المكدس المحلي

الأمر: `ops/scripts/Test-StagingLocalRuntime.ps1`

| خطوة | النتيجة |
|------|---------|
| `docker compose config` | PASS |
| بناء `smartsecurity-api:local-stg` | PASS (بعد تجاهل `global.json` داخل الصورة لأن SDK الحاوية 10.0.404) |
| postgres + api healthy، proxy running | PASS |
| API بلا `docker port` | PASS |
| `GET https://localhost:8443/health/live` عبر `--resolve` | **200** |
| `GET /health/ready` | **200** |
| `http://127.0.0.1:8080` → 301 إلى HTTPS | PASS |
| صف `ops_t03_probe` بعد restart | PASS |
| `docker compose down` بلا `-v` وبقاء `smartsecurity_stg_pgdata` | PASS |

شهادة Caddy `local_certs` مختبر فقط. ليست شهادة Staging عامة ولا تغني عن H1–H7.

---

## 4. ما بقي خارجياً

| البند | السجل | شرط الاستئناف |
|-------|--------|----------------|
| H1–H7 Cookie/CSRF/CORS/Origin على HTTPS عام | DEP-WEB-01 · DEP-OPS-03 | مضيف Staging + شهادة بيئة + تفويض تشغيل |
| تفعيل MQTT/Outbox إنتاجي | DEP-OPS-01 · G1 | ما زال DISABLED |

---

## 5. مهام يمكن البدء بها دون إغلاق OPS-T-03

| مهمة | العلاقة |
|------|---------|
| **INT-T-04** | مستقلة. تحتاج `START TASK: INT-T-04`. لا تبدأ تلقائياً. G1 تبقى BLOCKED حتى أدلتها. |
| **OPS-T-04** | النسخ والاستعادة. البطاقة التالية في Operations بعد T-03. لا تبدأ قبل إغلاق T-03 أو تفويض صريح. |
| ESP / G0 / G2 | ما زالت محجوبة بعتاد أو ببوابات. لا تبدأ من هذا التقرير. |

لم تُبدأ أي مهمة في هذه الجولة.

---

## 6. فجوات داخل النطاق

| فجوة | شدة | الحالة |
|------|-----|--------|
| HTTPS العام H1–H7 | High للقبول التشغيلي | مفتوحة — DEP-OPS-03 |
| ثقة Forwarded Headers المفتوحة | High | مغلقة في الكود |
| مفاتيح DataProtection داخل الحاوية غير دائمة | Low | موثّقة؛ ليست شرط إغلاق |

---

## 7. الحكم

**IMPLEMENTATION COMPLETE — EXTERNAL VALIDATION PENDING**

SOFTWARE COMPLETE لا يساوي STAGING VALIDATED. المهمة تبقى مفتوحة حتى موافقة صريحة بعد تحقق H1–H7 أو قرار مالك منفصل. لا إغلاق ذاتي.
