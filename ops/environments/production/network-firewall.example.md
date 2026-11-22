# Production — حدود شبكة (قالب OPS-T-02)

**الحالة:** قالب فقط. **MQTT الإنتاجي DISABLED.** لا تفعيل في OPS-T-02.

## مبادئ

- Deny-by-default على كل المنافذ غير 443 للعامة.
- PostgreSQL وMQTT داخل شبكة خاصة فقط.
- لا شهادات اختبار OPS-T-01.
- لا وصول مباشر من أجهزة مطوّرين إلى وسيط/قاعدة الإنتاج دون bastion بموافقة.

## قواعد مقترحة

| مصدر | مقصد | قرار |
|------|------|------|
| Internet | HTTPS reverse proxy | Allow 443 |
| Internet | DB / MQTT / admin ports | **Deny** |
| App tier | DB | Allow private |
| App tier | MQTT | Allow private **فقط بعد** تفعيل إنتاج مفوّض + TLS |
| Staging network | Production network | **Deny** (لا أمر جهاز إنتاج من بيئة اختبار) |

## فصل اعتمادات الأجهزة

هويات MQTT وكلمات مرور الأجهزة في الإنتاج **مستقلة** عن development/staging. إعادة استخدام `deva`/`devb` المحلية في الإنتاج **محظور**.
