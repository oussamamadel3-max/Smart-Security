# قائمة الخدمات المنشورة — OPS-T-03

تُقيَّد بما نُفّذ فعلياً في Backend. **لا** تُنشر عمال غير جاهزة.

| خدمة | تُدرج في قالب Staging؟ | ملاحظة |
|------|-------------------------|--------|
| ASP.NET API (JWT/جلسات/مالك/أجهزة قراءة/أوامر HTTP) | **نعم** | المصدر: `security_backEnd` |
| PostgreSQL | **نعم** | قاعدة منفصلة عن Development |
| Reverse proxy (Caddy) + TLS | **نعم** | حافة HTTPS |
| Mosquitto | اختياري (`profile: mqtt`) | مختبر ACL فقط؛ API MQTT **معطّل** |
| MQTT Inbound worker | **لا** | `Enabled=false` |
| MQTT Outbox / LivePublish | **لا** | معطّل حتى G1 + تفويض |
| FCM delivery worker | **لا** | Fake/معطّل |

نجاح `/health/live` ≠ اكتمال سلسلة الأمر/ACK/FCM.
