# أمان الويب لكل بيئة — CORS / Cookie / CSRF / Origin

يرتبط بتنفيذ Backend T-02 (`Auth:Cors:AllowedOrigins`, Cookie refresh, Signed CSRF).  
قوالب القيم: `environments/*/web-origins.example.json`.

## 1. مصفوفة البيئات

| إعداد | Development | Staging (قالب) | Production (قالب) |
|-------|-------------|----------------|-------------------|
| Origins | localhost فقط | مضيف staging HTTPS | مضيف app HTTPS |
| Cookie name | `esp32_rt_dev` | `esp32_rt_stg` | `esp32_rt` |
| Cookie Secure | حسب المضيف | **true** | **true** |
| SameSite | Lax | None (إن لزم cross-site) | None (إن لزم) |
| CSRF | موقعة بالجلسة | موقعة | موقعة |
| مشاركة جلسات مع بيئة أخرى | **ممنوع** | **ممنوع** | **ممنوع** |

## 2. تجهيز اختبار HTTPS الفعلي (للتنفيذ عند وجود مضيف — OPS-T-03)

لا يُشغَّل ضد إنتاج/staging حي في OPS-T-02 (لا مضيف). قائمة التجهيز:

1. Origin مسموح + credentials → نجاح CORS preflight حيث ينطبق.
2. Origin مرفوض → لا credentials / رفض Cookie channel (`AUTH_ORIGIN_REJECTED`).
3. Refresh بدون CSRF → رفض.
4. Refresh بـ CSRF صحيح → 200 + تدوير refresh.
5. Set-Cookie: `HttpOnly`, `Secure`, `Path=/api/auth` (أو المسار المعتمد).
6. لا ظهور Access/Refresh في سجلات الحاوية.

سكربت تحقق القوالب: `../scripts/Test-EnvIsolationDocs.ps1` يتأكد من وجود الحقول في JSON لكل بيئة.

## 3. Mobile مقابل Web

| عميل | Refresh | ملاحظات |
|------|---------|---------|
| Mobile | body | لا يعتمد CORS للمتصفح |
| Web | cookie + CSRF + Origin allowlist | يجب عزل origins لكل بيئة |
