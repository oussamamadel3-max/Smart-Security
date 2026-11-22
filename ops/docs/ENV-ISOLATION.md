# عزل البيئات — OPS-01 / NFR-002

المرجع التشغيلي لمهمة **OPS-T-02**. القوالب: [`../environments/`](../environments/).

## 1. ما يُفصل بين Development / Staging / Production

| مورد | يجب فصله | آلية |
|------|----------|------|
| PostgreSQL | نعم | مضيف/قاعدة/مستخدم مختلفون؛ لا restore متقاطع بلا موافقة |
| MQTT broker | نعم | وسيط + ACL + هويات أجهزة منفصلة |
| Firebase/FCM | نعم | مشروع ومفاتيح خدمة منفصلة |
| JWT signing | نعم | مفتاح + Issuer/Audience لكل بيئة |
| جلسات / Refresh | نعم | تُخزَّن في DB البيئة ⇒ لا مشاركة |
| CORS / Cookie domain | نعم | ملفات `web-origins.example.json` |
| شهادات TLS | نعم | اختبار محلي ≠ staging/prod |
| صور الحاويات | نعم | لا تُخبَّأ أسرار في الطبقات |

## 2. منع بيئة اختبار من أمر جهاز إنتاج

1. لا اتصال شبكة افتراضي من Compose المحلي أو staging إلى وسيط الإنتاج.
2. كلمات مرور MQTT وهويات الأجهزة غير قابلة لإعادة الاستخدام عبر البيئات.
3. Backend الإنتاجي يبقى `LivePublishEnabled=false` / `Inbound:Enabled=false` / `Outbox:Enabled=false` حتى تفويض صريح بعد G1.
4. قوائم CORS للإنتاج لا تتضمن `localhost`.

## 3. Development الحالي (OPS-T-01)

- Compose: `127.0.0.1` فقط للمنافذ المنشورة.
- أسرار محلية في `ops/.env` (gitignored) + User Secrets للـ API.
- MQTT محلي معزول؛ ليس إنتاجاً.

## 4. Staging / Production

قوالب فقط في OPS-T-02. النشر الحي = **OPS-T-03** بعد استيفاء الاعتماديات.
