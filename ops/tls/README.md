# TLS المحلي — OPS-T-01

## طبقات منفصلة (لا تُخلط)

| طبقة | المعنى | حالة OPS-T-01 |
|------|--------|----------------|
| أ) تجهيز شهادة مختبر | ملفات تحت `tls/server/` عبر `scripts/Generate-LocalTls.ps1` | مسموح |
| ب) تحقق معزول | `scripts/Test-LocalTls.ps1` | مسموح عند التشغيل |
| ج) أدلة G1 | `docs/gates/G1-evidence.md` | لا تُغلق هنا |
| د) تفعيل إنتاج | `Mqtt:UseTls` / LivePublish | **معطّل** — ممنوع في هذه المهمة |

## قواعد

- الشهادات والمفاتيح **gitignored** (`ops/.gitignore`).
- لا تنسخ `tls/server/*` إلى staging/production.
- لا تعطّل التحقق من الشهادة كبديل عن CA موثوق.
- نجاح الاتصال المحلي ≠ اجتياز G1 ≠ PRODUCTION READY.
