# عزل البيئات — OPS-T-02 (OPS-01)

ثلاث بيئات منطقية منفصلة. **لا** تُخلط قواعد البيانات أو وسيط MQTT أو Firebase أو اعتمادات الأجهزة أو مفاتيح JWT أو جلسات Refresh بينها.

| بيئة | الغرض | حالة التشغيل الحالية |
|------|--------|----------------------|
| `development` | مطوّر محلي + Compose OPS-T-01 | **نشطة محلياً** (`ops/docker-compose.yml`) |
| `staging` | قبول قبل الإنتاج | **قالب فقط** — لا نشر حي في هذه المهمة (OPS-T-03) |
| `production` | تشغيل حقيقي | **معطّل** — MQTT/Outbox/FCM إنتاجي DISABLED؛ قوالب فقط |

## قواعد العزل الإلزامية

1. **أسماء قواعد بيانات مختلفة** لكل بيئة (لا مشاركة `esp32_security` بين staging/prod).
2. **وسطاء MQTT منفصلون** (مضيف/منفذ/ACL/هويات أجهزة مختلفة). جهاز تطوير **ممنوع** من النشر على وسيط إنتاج.
3. **Firebase / FCM**: مشروع منفصل لكل بيئة؛ لا مشاركة مفاتيح خدمة.
4. **JWT SigningKey** مستقل لكل بيئة؛ Issuer/Audience يميّزان البيئة حيث أمكن.
5. **لا مشاركة** جداول جلسات / Refresh Tokens بين البيئات (قواعد منفصلة ⇒ عزل تلقائي).
6. **شهادات TLS اختبار** (OPS-T-01) **ممنوعة** في staging/production.
7. ملفات `*.env.example` هنا بلا أسرار حقيقية؛ القيم الحية في مدير أسرار / User Secrets / متغيرات بيئة المضيف فقط.

## محتويات كل مجلد

| ملف | الدور |
|-----|--------|
| `backend.env.example` | أسماء متغيرات Backend المطلوبة (فارغة القيم الحساسة) |
| `web-origins.example.json` | CORS / Cookie / CSRF لكل بيئة |
| `network-firewall.example.md` | حدود شبكة وجدار ناري (staging/prod) |

راجع أيضاً:

- [`../docs/ENV-ISOLATION.md`](../docs/ENV-ISOLATION.md)
- [`../docs/SECRETS-AND-JWT.md`](../docs/SECRETS-AND-JWT.md)
- [`../docs/WEB-SECURITY-PER-ENV.md`](../docs/WEB-SECURITY-PER-ENV.md)
