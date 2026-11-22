# اختبار HTTPS الحقيقي — قائمة قبول (OPS-T-03)

**Staging Runtime Validated** يتطلب مضيفاً ونطاقاً وشهادة حقيقية.  
المختبر المحلي (`Caddyfile` + `tls internal`) يثبت التوجيه فقط — **لا** يستبدل هذا الجدول على مضيف عام.

## سيناريوهات إلزامية على HTTPS staging

| # | السيناريو | نتيجة متوقعة |
|---|-----------|--------------|
| H1 | Origin مسموح + credentials | CORS OK |
| H2 | Origin مرفوض | رفض Cookie channel / لا credentials |
| H3 | Refresh بلا CSRF | رفض |
| H4 | Refresh + CSRF صحيح + Origin مسموح | 200 + تدوير refresh |
| H5 | Set-Cookie Refresh | `HttpOnly`; `Secure`; `Path` صحيح؛ SameSite وفق القالب |
| H6 | لا Access/Refresh في `docker logs` | PASS |
| H7 | HTTP plain إلى API العام | يُرفض أو يُحوَّل إلى HTTPS (لا جلسة Cookie على HTTP عام) |

## حالة التنفيذ الحالية

| بيئة | الحالة |
|------|--------|
| قوالب origins/cookie (OPS-T-02) | Configuration Validated |
| Caddy محلي tls internal | Configuration Validated / محلي اختياري |
| مضيف staging عام + شهادة | **NOT VERIFIED** — اعتماد خارجي |

عند الاستئناف: نفّذ H1–H7 وسجّل الأدلة في `docs/gates/G1-evidence.md` أو تقرير OPS دون أسرار.
