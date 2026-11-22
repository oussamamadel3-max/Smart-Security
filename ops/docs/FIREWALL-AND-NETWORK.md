# الشبكة وجدار الناري — OPS-03

## Development (OPS-T-01 — متحقق)

| خدمة | ربط المضيف | ملاحظة |
|------|------------|--------|
| PostgreSQL | `127.0.0.1:5433` | غير معرّض للعامة |
| MQTT plain | `127.0.0.1:1883` | مختبر محلي فقط |
| MQTT TLS | `127.0.0.1:8883` | شهادات اختبار |
| Docker network | `smartsecurity_local` | عزل بين حاويات المشروع |

التحقق:

```powershell
cd ops
docker compose --env-file .env ps
# يجب أن تظهر المنافذ على 127.0.0.1 وليس 0.0.0.0 إن أُعدّ كذلك في compose
```

## Staging / Production

قوالب القواعد في:

- `environments/staging/network-firewall.example.md`
- `environments/production/network-firewall.example.md`

لا تُطبَّق على مضيف حي في OPS-T-02.

## منع العبور

- لا تضع connection string الإنتاج في User Secrets للتطوير.
- لا تفتح نفق SSH دائم من جهاز المطوّر إلى MQTT الإنتاج دون موافقة.
- ACL المحلي (`deva`/`devb`) لا يُنسخ إلى وسيط إنتاج.
