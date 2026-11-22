# OPS-T-01 — تقرير قبول بيئة التطوير المحلية (Docker Compose)

| الحقل | القيمة |
|--------|--------|
| المهمة | `OPS-T-01` / `T-01` |
| التاريخ | 2026-11-22 |
| الحكم | **CLOSED** (بعد تحقق التقرير وموافقة الإغلاق) |
| إغلاق رسمي | **مغلق** — `APPROVE TASK CLOSURE: OPS-T-01` · انظر `OPS-T-01-CLOSURE-REPORT.md` |
| العدّاد الرسمي بعد الإغلاق | **23/50** · Operations **1/6** |
| البوابات | G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` |

---

## 1. نطاق المهمة الرسمي

من `ops/ops-development-tasks.md` و`ops-current.md`:

- Docker Compose لـ PostgreSQL + Mosquitto (أو مكافئ موثّق) مع TLS تجريبي وACL.
- Volumes دائمة + healthchecks + تعليمات تشغيل.
- اختبار عميلين تجريبيين A/B: سماح قنوات الذات، رفض عبور الأجهزة، رفض المجهول (نشر واشتراك).
- لا أسرار إنتاج في Git؛ اسم/إصدار صورة الوسيط موثّقان.
- **خارج النطاق:** OPS-T-02…، INT-T-02، تفعيل MQTT/Outbox إنتاجي، عتاد ESP32، Flutter، إغلاق G0/G1/G2.

---

## 2. حالة البيئة قبل التنفيذ

| بند | النتيجة |
|-----|---------|
| مجلد `ops/` | تخطيط فقط — لا Compose |
| Docker CLI | موجود (`Docker Desktop` تحت `%LOCALAPPDATA%\Programs\DockerDesktop`) |
| Docker daemon | كان متوقفاً عند الجرد؛ شُغِّل لاحقاً بنجاح (Engine **29.9.3**) |
| صور متوفرة مسبقاً | `postgres:16-alpine`, `eclipse-mosquitto:2` |
| منفذ 5432 على المضيف | مشغول بـ PostgreSQL محلي (PID مضيف) — لذلك اختير **5433** للحاوية |
| منافذ 1883/8883/5433 | كانت فارغة |
| مرجع ACL | `security_backEnd/API.IntegrationTests/mosquitto/{acl,mosquitto.conf}` |
| Backend MQTT إنتاجي | `Enabled/LivePublish/Outbox = false` — لم يُمس |

---

## 3. الأعمال الفرعية المنفذة

| فرع | الحالة |
|-----|--------|
| A. Docker Compose | منفَّذ |
| B. PostgreSQL | منفَّذ |
| C. Eclipse Mosquitto + ACL | منفَّذ |
| D. TLS محلي (طبقات أ/ب) | منفَّذ |
| E. Configuration & Secrets | منفَّذ (`.env.example` + gitignore) |
| F. توافق Backend (توثيق اتصال دون تفعيل حي) | منفَّذ توثيقياً |
| توثيق README/اختبارات | منفَّذ |

---

## 4. الخدمات والإعدادات المنشأة

| خدمة | صورة | حاوية | منافذ المضيف | Volume |
|------|------|--------|--------------|--------|
| postgres | `postgres:16-alpine` | `smartsecurity-postgres` | `127.0.0.1:5433→5432` | `smartsecurity_pgdata` |
| mosquitto | `eclipse-mosquitto:2` (broker **2.1.2**) | `smartsecurity-mosquitto` | `127.0.0.1:1883→1883`, `127.0.0.1:8883→8883` | `smartsecurity_mosquitto_data` |

- شبكة: `smartsecurity_local` (bridge)
- مشروع Compose: `smartsecurity`
- سياسة إعادة التشغيل: `unless-stopped`
- Healthchecks: `pg_isready`؛ Mosquitto عبر `mosquitto_pub` بهوية `srv`

---

## 5. الملفات الجديدة والمعدّلة

### جديدة

- `ops/docker-compose.yml`
- `ops/.env.example`
- `ops/.gitignore`
- `ops/README.md`
- `ops/mosquitto/mosquitto.conf`
- `ops/mosquitto/acl`
- `ops/tls/README.md`
- `ops/scripts/Bootstrap-LocalStack.ps1`
- `ops/scripts/Generate-LocalTls.ps1`
- `ops/scripts/Test-LocalStack.ps1`
- `ops/scripts/Test-AclMatrix.ps1`
- `ops/scripts/Test-LocalTls.ps1`
- `ops/scripts/mqtt_acl_probe.py`
- `docs/reports/ops/OPS-T-01-report.md`

### معدّلة (تتبّع — بلا `[x]` إغلاق)

- `ops/ops-current.md`, `ops-context.md`, `ops-progress.md`, `ops-development-tasks.md` (نص الحالة فقط)
- `system-context.md`, `system-progress.md` (العدّاد **22/50** وOps **0/6** ثابتان)
- `docs/gates/G1-evidence.md`, `docs/gates/G0-G1-remaining-dependencies.md`

### gitignored (مولَّدة محلياً)

- `ops/.env`, `ops/mosquitto/passwd`, `ops/tls/server/*`

---

## 6. مخطط الخدمات والشبكات المحلي

```text
Host (127.0.0.1 only)
 ├── :5433 ──► smartsecurity-postgres  (smartsecurity_pgdata)
 ├── :1883 ──► smartsecurity-mosquitto  (plaintext + ACL)
 └── :8883 ──► smartsecurity-mosquitto  (TLS test CA + ACL)
         │
         └── network: smartsecurity_local
```

لا منافذ منشورة على `0.0.0.0`. لا خدمة Backend داخل Compose في هذه المهمة.

---

## 7. إعدادات PostgreSQL

| بند | قيمة |
|-----|------|
| صورة | `postgres:16-alpine` |
| قاعدة | `esp32_security_dev` |
| مستخدم | `ss_dev` |
| كلمة المرور | من `ops/.env` فقط (مثال في `.env.example`) |
| منفذ مضيف | 5433 (تجنب تعارض 5432) |
| مخطط EF | لم يُنشأ مخطط بديل؛ Migrations اختيارية لاحقاً على DB معزولة |
| بيانات إنتاج | غير مستخدمة |

سلسلة اتصال مقترحة للتطوير (User Secrets — لا تُرفع):

`Host=localhost;Port=5433;Database=esp32_security_dev;Username=ss_dev;Password=<local>`

---

## 8. إعدادات Mosquitto

| بند | قيمة |
|-----|------|
| صورة / إصدار | `eclipse-mosquitto:2` / **2.1.2** |
| `allow_anonymous` | `false` |
| مستمعون | 1883 نصي · 8883 TLS (`require_certificate false`) |
| مصادقة | `password_file` لهويات `srv`/`deva`/`devb` |
| ACL | `mosquitto/acl` وفق مسودة MQTT v1 |
| persistence | `true` → `/mosquitto/data` (volume) |
| نقطة التشغيل | نسخ RO → مسارات مملوكة لـ `mosquitto` (صلاحيات Windows bind-mount) |

---

## 9. مصفوفة ACL التي جرى اختبارها

أداة التحقق: `ops/scripts/mqtt_acl_probe.py` (paho MQTT v5) عبر `Test-AclMatrix.ps1`.  
ملاحظة: `mosquitto_pub` قد يعرض نجاحاً رغم PUBACK **135**؛ لذلك لا يُعتمد وحده.

| Id | هوية | عملية | قناة | متوقع | فعلي | نتيجة |
|----|------|--------|------|--------|------|--------|
| anonymous_publish_rejected | (none) | publish | `devices/{A}/state` | deny | CONNACK 135 | PASS |
| deva_publish_own_state | deva | publish | `devices/{A}/state` | allow | PUBACK 16 | PASS |
| deva_publish_devb_state_denied | deva | publish | `devices/{B}/state` | deny | PUBACK 135 | PASS |
| deva_publish_commands_denied | deva | publish | `devices/{A}/commands` | deny | PUBACK 135 | PASS |
| srv_publish_deva_command | srv | publish | `devices/{A}/commands` | allow | PUBACK 0 | PASS |
| deva_receives_own_command | deva | subscribe | `devices/{A}/commands` | allow | MESSAGE … | PASS |
| deva_does_not_receive_devb_commands | deva | subscribe | `devices/{B}/commands` | deny | NO_MESSAGE | PASS |
| devb_publish_own_events | devb | publish | `devices/{B}/events` | allow | PUBACK 16 | PASS |
| bad_password_rejected | deva+bad | connect | — | deny | CONNACK 135 | PASS |

**المجموع: 9/9 PASS** (إعادة تشغيل موثّقة 2026-11-22).

معرّفات الأجهزة التجريبية:  
`11111111-1111-4111-8111-111111111111` / `22222222-2222-4222-8222-222222222222`.

---

## 10. نتائج اختبارات Compose

| اختبار | أمر/أسلوب | نتيجة |
|--------|-----------|--------|
| صحة التكوين | `docker compose --env-file .env config --quiet` | PASS |
| إقلاع | `docker compose up -d` | PASS |
| Health | `postgres=healthy`, `mosquitto=healthy` | PASS |
| إعادة تشغيل | `docker compose restart` + بقاء صف `ops_t01_*` | PASS |
| إيقاف دون حذف بيانات | `docker compose down` ثم `up -d`؛ volumes باقية؛ الصف `ops_t01_c7fcec3e` موجود | PASS |

---

## 11. نتائج اختبار الاتصال والخدمات

| خدمة | الدليل | نتيجة |
|------|--------|--------|
| PostgreSQL داخل الحاوية | `psql` CREATE/INSERT | PASS |
| PostgreSQL من المضيف عبر 5433 | عميل `postgres:16-alpine` → `host.docker.internal:5433` | PASS |
| Mosquitto نصي | ACL matrix + healthcheck `srv` | PASS |
| Mosquitto TLS | `Test-LocalTls.ps1` | PASS |

---

## 12. نتائج الاختبارات الأمنية

| فحص | نتيجة |
|-----|--------|
| رفض مجهول | PASS (CONNACK 135) |
| عزل جهاز↔جهاز (نشر) | PASS (PUBACK 135) |
| منع الجهاز من `commands` | PASS |
| عزل اشتراك أوامر الجهاز الآخر | PASS (NO_MESSAGE) |
| كلمة مرور خاطئة | PASS |
| TLS + CA موثوق | PASS (PUBACK 16) |
| TLS بلا CA | PASS (رفض) |
| TLS + CA غير موثوق | PASS (`CERTIFICATE_VERIFY_FAILED`) |
| عدم تعطيل التحقق من الشهادة | PASS (التحقق مفعّل) |

**لا يُخلط** مع اجتياز G1-03 أو جاهزية الإنتاج.

---

## 13. التحقق من عدم تعرّض الأسرار

| فحص | نتيجة |
|-----|--------|
| `.env` في `.gitignore` | نعم |
| `mosquitto/passwd` gitignored | نعم |
| `tls/server/*` gitignored | نعم |
| Compose المتتبَّع بلا كلمات مرور ثابتة | نعم (`${VAR:?…}`) |
| التقارير بلا كلمات مرور | نعم |
| شهادات اختبار ≠ إنتاج | موثّق في `ops/tls/README.md` |

---

## 14. الفجوات والاعتماديات المتبقية

- إغلاق رسمي لـ OPS-T-01 معلّق على موافقة المالك.
- OPS-T-02 (عزل بيئات/أسرار staging-prod) لم يبدأ.
- تفعيل MQTT/Inbound/Outbox/LivePublish الإنتاجي **ممنوع** حتى أدلة G1 + تفويض.
- G0 / D-01 عتاد ما زال معلّقاً.
- INT-T-02 يمكن أن يستفيد من المكدس بعد إغلاق OPS-T-01؛ لم يُبدأ هنا.
- جدول `ops_t01_smoke` للاختبار فقط — ليس جزءاً من نموذج EF.

---

## 15. مصفوفة شروط القبول ودليل كل شرط

| شرط القبول | الدليل | التصنيف |
|------------|--------|---------|
| مطوّر يشغّل المكدس محلياً | `ops/README.md` + `Bootstrap` + `compose up` | VERIFIED |
| وصول API إن شُغّل منفصلاً | توثيق سلسلة 5433؛ API خارج Compose | PARTIAL (توثيق؛ تشغيل API ليس ضمن بطاقة OPS) |
| وصول broker بجهاز تجريبي | ACL A/B عبر paho | VERIFIED |
| بلا أسرار إنتاج في Git | gitignore + مراجعة الملفات المتتبَّعة | VERIFIED |
| اسم/إصدار صورة الوسيط موثّقان | `eclipse-mosquitto:2` / 2.1.2 في README والتقرير | VERIFIED |
| ACL سماح/منع بالاتجاهين | جدول §9 — 9/9 | VERIFIED |
| استمرار volumes بعد إعادة تشغيل | restart + down/up | VERIFIED |
| TLS تجريبي | §12 | VERIFIED (طبقة أ/ب فقط) |
| لا تفعيل إنتاج MQTT | لم تُعدَّل `appsettings` الحيّة | VERIFIED |

---

## 16. أثر المهمة على G0 وG1 وG2

| بوابة | الأثر |
|-------|--------|
| G0 | لا تغيير — `NOT CLOSED` |
| G1 | لا إغلاق — `BLOCKED`؛ أُضيف سجل أدلة محلية في `G1-evidence.md` صراحةً بأنها ليست إغلاقاً |
| G2 | لا تغيير — `NOT CLOSED` |

---

## 17. العدّادات الرسمية قبل وبعد

| مؤشر | قبل | بعد (هذا التقرير) |
|------|-----|-------------------|
| النظام | 22/50 | **23/50** (عند الإغلاق) |
| Operations | 0/6 | **1/6** (عند الإغلاق) |
| Integration | 1/6 | 1/6 |
| بطاقات T-01 `[x]` | فارغة | **معلَّمة** عند الإغلاق |

---

## 18. شروط بدء المهمة التالية وفق الاعتماديات

1. **لإغلاق OPS-T-01:** رسالة المالك `APPROVE TASK CLOSURE: OPS-T-01`.
2. **OPS-T-02:** يمكن بدؤها بعد إغلاق T-01 (عزل أسرار/بيئات) — لا تُبدأ تلقائياً.
3. **INT-T-02:** وفق تسلسل Integration بعد توفر مكدس محلي؛ لا تُبدأ في هذا التقرير.
4. لا تفعيل مسارات أوامر/حوادث إنتاج قبل G1.

---

## 19. درس هندسي مختصر

**المفهوم:** الاعتماد على رمز سبب MQTT v5 (مثل PUBACK **135** Not Authorized و**16** No matching subscribers) بدل رمز خروج `mosquitto_pub` عند التحقق من ACL.

**لماذا يعمل:** Mosquitto قد يُرجع PUBACK بالرفض بينما عميل سطر الأوامر يُظهر نجاحاً مضللاً؛ عميل يقرأ Reason Code يثبت السماح/المنع فعلياً.

**متى يُستخدم:** أي اختبار عزل قنوات أو صلاحيات نشر على Mosquitto 2.x / MQTT 5.

**تحقق عملي:** `ops/scripts/mqtt_acl_probe.py` + مقارنة `PUBACK 135` لحالة الرفض و`0|16` للسماح في `tmp/acl-matrix-result.csv`.

---

## الحكم النهائي

**CLOSED** — 2026-11-22

التنفيذ والاختبارات التشغيلية الإلزامية ضمن نطاق OPS-T-01 مستوفاة.  
صدرت الموافقة: `APPROVE TASK CLOSURE: OPS-T-01` بعد تحقق التقرير من شروط Compose/PostgreSQL/Mosquitto/ACL/TLS بلا فجوات مانعة.  
تفاصيل المزامنة: `OPS-T-01-CLOSURE-REPORT.md`. لا بدء تلقائي للمهمة التالية.
