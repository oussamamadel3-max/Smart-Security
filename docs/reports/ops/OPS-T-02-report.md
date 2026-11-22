# OPS-T-02 — تقرير قبول عزل البيئات والأسرار

| الحقل | القيمة |
|--------|--------|
| المهمة | `OPS-T-02` / `T-02` |
| التاريخ | 2026-11-22 |
| الحكم | **CLOSED** (بعد موافقة المالك) |
| إغلاق رسمي | **مغلق** — `APPROVE TASK CLOSURE: OPS-T-02` · انظر `OPS-T-02-CLOSURE-REPORT.md` |
| العدّاد بعد الإغلاق | **26/50** · Ops **2/6** |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED` |

---

## 1. نص البطاقة الرسمية ونطاقها

من `ops/ops-development-tasks.md` — **T-02 عزل البيئات والأسرار**:

- **SRS:** `NFR-002`؛ `OPS-01` `OPS-03`
- **المطلوب:** فصل dev/staging/production (DB، وسيط، Firebase، اعتمادات أجهزة)؛ أسرار خارج Git/الصور/السجلات؛ TLS؛ جدار ناري؛ منع اختبار من أمر جهاز إنتاج؛ JWT Signing Key خارج Git بمفتاح لكل بيئة + سياسة تدوير/`kid`/إبطال؛ User Secrets للتطوير فقط؛ عزل CORS/Cookie/CSRF/Origin لكل بيئة بلا مشاركة جلسات.
- **القبول:** لا أسرار في Git/صور/سجلات؛ لا اتصال متقاطع؛ خطة تدوير JWT موثّقة + إعادة تشغيل آمن؛ صلاحية إعدادات الويب لكل بيئة.
- **البطاقات:** OPS-01 · OPS-03 · قائمة تحقق أسرار · JWT تدوير/إبطال · CORS/Cookie/CSRF + تجهيز HTTPS فعلي.

**خارج النطاق:** نشر staging حي (OPS-T-03) · تفعيل MQTT إنتاجي · تعديل Stage/Prod حي · عتاد · INT-T-04 · إغلاق ذاتي.

---

## 2. حالة المشروع والاعتماديات عند البدء

| بند | القيمة الفعلية |
|-----|----------------|
| Official Progress | **25/50 (50%)** |
| Integration / Ops / BE / ESP / Mobile / Web | 3/6 · **1/6** · 5/13 · 0/9 · 8/8 · 8/8 |
| INT-T-03 | ✓ مغلقة (`INT-T-03-CLOSURE-REPORT.md`) — ليست مانعاً لـ OPS-T-02 |
| OPS-T-01 | ✓ مغلقة — Compose محلي healthy |
| G0 / G1 / G2 / MQTT Prod | NOT CLOSED / BLOCKED / NOT CLOSED / DISABLED |

---

## 3. الأعمال الفرعية المطلوبة (مصفوفة)

| بطاقة | مخرج مطلوب | دليل القبول |
|-------|------------|-------------|
| OPS-01 بيئات منفصلة | قوالب `environments/*` + `ENV-ISOLATION.md` | وجود الملفات + اختبار القوالب |
| OPS-03 أسرار/TLS/جدار ناري | `SECRETS-AND-JWT` · `FIREWALL-AND-NETWORK` · قوالب شبكة | وثائق + hygiene |
| لا سر في صورة/سجل | `SECRETS-CHECKLIST` + `Test-SecretsHygiene.ps1` | exit 0 |
| JWT فصل/تدوير/إبطال | سياسة موثّقة + User Secrets للتطوير | وثيقة + SigningKey فارغ في Git |
| CORS/Cookie/CSRF لكل بيئة + تجهيز HTTPS | `web-origins.example.json` ×3 + `WEB-SECURITY-PER-ENV.md` | اختبار قوالب؛ HTTPS حي = لاحقاً |

---

## 4. ما كان موجوداً قبل التنفيذ

- OPS-T-01: Compose + Postgres/Mosquitto + ACL/TLS محلي + `.gitignore` للأسرار المحلية.
- Backend: `SigningKey=""` في `appsettings.json`؛ CORS localhost في التطوير؛ MQTT flags disabled.
- لا قوالب staging/production منظمة تحت `ops/environments/`.

---

## 5. الأعمال المنفَّذة فعلياً

1. إنشاء شجرة `ops/environments/{development,staging,production}/` (backend.env / web-origins / firewall).
2. وثائق تشغيلية تحت `ops/docs/`.
3. سكربتات: `Test-SecretsHygiene.ps1` · `Test-EnvIsolationDocs.ps1` · `Test-OpsT02.ps1`.
4. توسيع `ops/.gitignore` وربط OPS-T-02 في `ops/README.md`.
5. انحدار OPS-T-01 (health + ACL 9/9) دون حذف volumes.
6. تحديث تتبّع بلا `[x]`/عدّادات.

---

## 6. الملفات المنشأة والمعدّلة

### جديدة

- `ops/environments/**`
- `ops/docs/ENV-ISOLATION.md` · `SECRETS-AND-JWT.md` · `SECRETS-CHECKLIST.md` · `WEB-SECURITY-PER-ENV.md` · `FIREWALL-AND-NETWORK.md`
- `ops/scripts/Test-SecretsHygiene.ps1` · `Test-EnvIsolationDocs.ps1` · `Test-OpsT02.ps1`
- `docs/reports/ops/OPS-T-02-report.md`

### معدّلة

- `ops/README.md` · `ops/.gitignore`
- `ops/ops-*.md` (حالة بلا إغلاق رسمي)
- `system-context.md` · `system-progress.md` (مؤشر؛ **25/50** ثابت)
- `docs/gates/G1-evidence.md` · `G0-G1-remaining-dependencies.md` (DEP-WEB-01)

---

## 7. الإعدادات والخدمات المتأثرة

| عنصر | أثر OPS-T-02 |
|------|----------------|
| Docker Compose OPS-T-01 | بلا تغيير دلالي — انحدار healthy |
| `appsettings.json` | لم يُعدَّل (SigningKey يبقى فارغاً) |
| MQTT إنتاجي | يبقى DISABLED في القوالب والملتزم |
| Staging/Prod hosts | **لم تُنشأ** — قوالب فقط |

---

## 8. قرارات ضمن النطاق

1. Staging/Production = قوالب توثيقية حتى OPS-T-03 (قيود البرومبت تمنع النشر الحي).
2. سياسة JWT توثّق `kid`/نافذة مزدوجة؛ Backend الحالي مفتاح واحد — يُسجَّل كفجوة تنفيذ لاحقاً دون تغيير عقد دلالي هنا.
3. شهادات OPS-T-01 اختبار فقط؛ ممنوعة في قوالب prod/staging.
4. أسماء كوكي مختلفة بين البيئات لمنع التباس المتصفح.

---

## 9. النتائج التشغيلية

- `smartsecurity-postgres` / `smartsecurity-mosquitto`: **healthy**
- منافذ التطوير تبقى على الحلقة المحلية حسب OPS-T-01
- لا تفعيل LivePublish/Inbound/Outbox

---

## 10. نتائج الاختبارات

| اختبار | الأمر | البيئة | النتيجة |
|--------|-------|--------|---------|
| Secrets hygiene | `.\scripts\Test-SecretsHygiene.ps1` | repo | **PASS** (7 فحوصات) |
| Env templates | `.\scripts\Test-EnvIsolationDocs.ps1` | repo | **PASS** |
| Compose health | ضمن `Test-OpsT02.ps1` | Docker محلي | **PASS** |
| ACL regression | `Test-AclMatrix.ps1` عبر OpsT02 | Mosquitto محلي | **9/9 PASS** |
| AccountAccessRules | `dotnet test … AccountAccessRulesTests` | Backend unit | **3/3 PASS** |
| HTTPS حي staging | — | لا مضيف | **NOT RUN** (مجهَّز) |
| إعادة تشغيل API بـ User Secrets تفاعلية | — | — | **NOT RUN** هذه الجلسة؛ بوابة المفتاح الفارغ في Git **VERIFIED** |

---

## 11. نتائج أمنية (ضمن النطاق)

| فحص | نتيجة |
|-----|--------|
| لا `.env` / `passwd` / مفاتيح TLS متتبَّعة | PASS |
| SigningKey فارغ في Git | PASS |
| MQTT إنتاجي disabled في الملتزم | PASS |
| قوالب prod/staging بلا localhost origins | PASS |
| فصل cookie names عبر البيئات | PASS |

---

## 12–13. المخاطر والفجوات

| فجوة | معالجة / تأجيل |
|------|----------------|
| لا مضيف staging HTTPS | مؤجّل OPS-T-03 — التجهيز مكتمل |
| Backend بلا مصفوفة `kid` متعددة | موثّق في `SECRETS-AND-JWT.md` — تحسين Backend لاحق |
| إعادة تشغيل API الحي بمفتاح User Secrets | إجراء مطوّر محلي؛ غير مُنفَّذ في هذه الجلسة |
| DEP-OPS-01 تفعيل إنتاج | خارج النطاق — DISABLED |
| صور حاوية API للإنتاج | لا صورة نشر بعد — قائمة التحقق جاهزة عند البناء |

---

## 14. مصفوفة شروط القبول

| شرط | التصنيف | الدليل |
|-----|---------|--------|
| لا أسرار في Git | **VERIFIED** | Test-SecretsHygiene |
| لا أسرار في صور/سجلات (سياسة + gitignore + لا أسرار في الأمثلة) | **VERIFIED** (صور إنتاج غير مبنية بعد) | Checklist + scripts |
| لا اتصال متقاطع بيئات / صلاحيات أجهزة | **VERIFIED** (سياسة + قوالب + ACL محلي 9/9) | ENV-ISOLATION + ACL |
| خطة تدوير JWT موثّقة | **VERIFIED** | SECRETS-AND-JWT.md |
| اختبار إعادة تشغيل آمن | **PARTIAL** | بوابة المفتاح في Git ✓؛ عملية API حية NOT RUN |
| إعدادات ويب لكل بيئة صالحة | **VERIFIED** (قوالب) | Test-EnvIsolationDocs |
| تجهيز اختبار HTTPS فعلي | **VERIFIED** | web-origins staging checklist |
| تنفيذ HTTPS حي | **NOT VERIFIED / BLOCKED** | لا مضيف — OPS-T-03 |

ضمن قيود المهمة (منع النشر الحي)، شروط البطاقة الجوهرية مستوفاة مع توثيق صريح لما هو مؤجّل.

---

## 15. ما تعذّر وأسبابه

| بند | السبب |
|-----|--------|
| اختبار Cookie/CSRF/CORS على HTTPS staging | لا مضيف staging؛ محظور نشر خارجي في البرومبت |
| تفعيل TLS إنتاجي MQTT | MQTT Production DISABLED |

---

## 16. أثر البوابات

| بوابة | الأثر |
|-------|--------|
| G0 | لا تغيير |
| G1 | لا تغيير — BLOCKED؛ دليل إضافي في G1-evidence فقط |
| G2 | لا تغيير |
| MQTT Production | يبقى DISABLED |

---

## 17. العدادات

| العداد | قبل | بعد التنفيذ (الآن) |
|--------|-----|---------------------|
| النظام | 25/50 | **25/50** |
| Ops | 1/6 | **1/6** |
| Integration | 3/6 | 3/6 |

بعد الموافقة فقط: Ops → 2/6 · النظام → 26/50.

---

## 18. جاهزية الإغلاق

**READY TO CLOSE**

التنفيذ ضمن النطاق مكتمل بأدلة محلية. HTTPS الحي صراحةً خارج المتاح ومجهَّز لـ OPS-T-03. لا إغلاق ذاتي.

---

## 19. المهمة التالية

| مهمة | ملاحظة |
|------|--------|
| OPS-T-03 | تعتمد على T-02؛ نشر/صحة — بانتظار START بعد الإغلاق |
| INT-T-04 | مستقلة نسبياً؛ بانتظار START منفصل |

لا بدء تلقائي.

---

## 20. درس هندسي

**المفهوم:** عزل البيئات يبدأ بـ«عقود إعداد» قابلة للتحقق آلياً (قوالب + سكربت نظافة) قبل وجود مضيف staging.

**لماذا ينجح:** يمنع تسريب أسرار وخلط origins قبل أن يُبنى النشر؛ ويحافظ على انحدار OPS-T-01.

**متى:** قبل أي نشر (OPS-T-03) وبعد ثبات Compose المحلي.

**تحقق:** `.\scripts\Test-OpsT02.ps1` يجب أن يخرج 0 مع ACL 9/9 وSigningKey فارغ في Git.

---

## انتظار المالك

```text
APPROVE TASK CLOSURE: OPS-T-02
```
