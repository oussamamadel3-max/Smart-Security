# المهمة الحالية — التشغيل والبيئات

status: implementation_complete_external_validation_pending — awaiting APPROVE TASK CLOSURE: OPS-T-03
task: نشر الخدمات والصحة (OPS-02 / OPS-04 / OPS-05)
srs: خطة OPS-02…05
parent: T-03 / deploy & health
branch: (محلي)
worktree: C:/Users/Osama/Desktop/SmartSecurity/ops

## المطلوب (من البطاقة)

1. OPS-02 نشر معزول + MQTT/TLS (قالب؛ MQTT API معطّل)
2. OPS-04 بدء/إيقاف وصحة ومساحات دائمة
3. OPS-05 إصدار محدد + خطة رجوع
4. اختبار HTTPS حقيقي Cookie/CSRF/CORS
5. probes منفصلة live/ready + فشل DB
6. قائمة خدمات مقيّدة بما نُفّذ

## تم (برمجي/محلي — بلا إغلاق)

- [x] `/health/live` و`/health/ready` + Forwarded Headers
- [x] `ops/staging/*` Compose/Dockerfile/Caddy/ROLLBACK/HTTPS-ACCEPTANCE
- [x] سكربتات `Test-OpsT03*.ps1`
- [x] HealthProbeTests 3/3 + ForwardedHeadersSetupTests 3/3
- [x] بناء صورة `smartsecurity-api:local-stg` وإقلاع المكدس محلياً (`Test-StagingLocalRuntime.ps1` PASS)
- [ ] HTTPS حي H1–H7 على مضيف staging عام — **NOT VERIFIED** (DEP-OPS-03 / DEP-WEB-01)

## تحقق

- [x] `Test-OpsT03.ps1` PASS (config/network/https-checklist/secrets)
- [x] Local Runtime: live/ready عبر HTTPS مختبر، restart، volume بعد `down` بلا `-v`
- [ ] `APPROVE TASK CLOSURE: OPS-T-03` — معلّق؛ المالك لم يوافق على الإغلاق
- الحكم: **IMPLEMENTATION COMPLETE — EXTERNAL VALIDATION PENDING**
- **SOFTWARE COMPLETE ≠ STAGING VALIDATED**

## ثوابت

G0=`NOT CLOSED` · G1=`BLOCKED` · G2=`NOT CLOSED` · MQTT Production=`DISABLED`  
Official **26/50** · Ops **2/6** حتى موافقة الإغلاق.

## التالي بعد الإغلاق فقط

- OPS-T-04 و/أو INT-T-04 بتفويض START — **لا بدء تلقائي**.
