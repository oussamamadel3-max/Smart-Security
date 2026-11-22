# الأسرار ومفاتيح JWT — OPS-03 / Backend T-02 ops overlay

## 1. مصادر الأسرار حسب البيئة

| بيئة | المصدر المسموح | ممنوع |
|------|----------------|-------|
| Development | .NET **User Secrets** (`esp32-security-backend-api`) · `ops/.env` (gitignored) · env vars محلية | Git · صور Docker · سجلات عامة · `appsettings*.json` بقيم حية |
| Staging | مدير أسرار المضيف / CI secrets / env للمضيف | Git · نسخ من Development · شهادات OPS-T-01 |
| Production | مدير أسرار الإنتاج فقط | كل ما سبق + أي مفتاح staging |

`API/appsettings.json` يبقي `Auth:Jwt:SigningKey` **فارغاً** عمداً. الإقلاع يرفض مفتاحاً أقصر من 32 حرفاً (`InfrastructureServiceRegistration`).

## 2. إعداد تطوير محلي (User Secrets)

من مجلد `security_backEnd/API`:

```powershell
dotnet user-secrets set "Auth:Jwt:SigningKey" "<local-dev-key-min-32-chars>"
dotnet user-secrets set "Auth:Jwt:Issuer" "esp32-security-dev"
dotnet user-secrets set "Auth:Jwt:Audience" "esp32-security-clients-dev"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=127.0.0.1;Port=5433;Database=esp32_security_dev;Username=ss_dev;Password=<from-ops-env>"
```

لا تطبع المفاتيح في التقارير أو الدردشة.

## 3. سياسة تدوير JWT (موثّقة)

Backend الحالي يتحقق بمفتاح متماثل واحد (`SymmetricSecurityKey`) **بدون** مصفوفة `kid` متعددة في الكود بعد. سياسة التشغيل لـ OPS-T-02:

| مرحلة | إجراء |
|-------|--------|
| تسمية منطقية | كل بيئة لها `SigningKey` مستقل؛ يُفضَّل تسجيل `kid` في مدير الأسرار كمفتاح بيانات التعريف حتى يدعم Backend اختيار مفتاح متعدد |
| تدوير مخطط | 1) توليد مفتاح جديد في مدير الأسرار 2) نشر يدعم التحقق بالمفتاحين (يتطلب تحسين Backend عند اعتماده) أو نافذة صيانة قصيرة 3) إيقاف المفتاح القديم بعد انتهاء Access TTL + هامش |
| تحقق مرحلي قبل إسقاط القديم | إصدار Access جديد بالمفتاح الجديد؛ رفض التوقيع بالمفتاح المسحوب بعد النافذة |
| إبطال اضطراري | `logout-all` للمستخدمين المتأثرين + تدوير فوري + إلغاء جلسات DB (Refresh) |
| استرجاع سر بيئة | استعادة من مدير الأسرار فقط؛ إعادة تشغيل API؛ التحقق أن `SigningKey` غير فارغ وأن اللوجين ينجح |

**اختبار إعادة تشغيل آمن (Development):** بعد ضبط User Secrets، أعد تشغيل API محلياً وتأكد أن الإقلاع لا يسقط بسبب مفتاح ناقص، وأن `/api/auth/login` يعمل — دون تسجيل المفتاح.

## 4. تدوير اعتمادات MQTT / أجهزة (D-04 يدوي لـ v1)

- هويات `srv`/`deva`/`devb` محلية فقط.
- staging/prod: هويات مختلفة؛ تدوير يدوي موثّق؛ لا Wi-Fi password على الخادم.
- بعد التدوير: حدّث `passwd`/مدير الأسرار وأعد تحميل الوسيط؛ اختبر ACL على عينة جهاز.

## 5. TLS

| طبقة | سياسة |
|------|--------|
| محلي OPS-T-01 | شهادات اختبار في `ops/tls/` (gitignored) |
| Staging/Prod | شهادات بيئة حقيقية؛ **ممنوع** نسخ اختبار → إنتاج (DEP-OPS-01) |
| MQTT إنتاجي | DISABLED؛ عند التفعيل لاحقاً: TLS إلزامي بلا استثناء |

## 6. جدار ناري

انظر [`FIREWALL-AND-NETWORK.md`](./FIREWALL-AND-NETWORK.md) وقوالب `environments/*/network-firewall.example.md`.
