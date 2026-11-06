# أوامر الترحيل والاختبار المحلي

## إنشاء ترحيل جديد (بعد تغيير النموذج)

```bash
dotnet ef migrations add <Name> --project Infrastructure --startup-project API --output-dir Persistence/Migrations --context AppDbContext
```

## تطبيق الترحيل على قاعدة محلية

```bash
dotnet ef database update --project Infrastructure --startup-project API --context AppDbContext
```

أو عبر متغير بيئة لتجاوز `appsettings.json`:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=esp32_security;Username=postgres;Password=<YOUR_PASSWORD>"
dotnet ef database update --project Infrastructure --startup-project API --context AppDbContext
```

## سكربت SQL للمراجعة

```bash
dotnet ef migrations script --project Infrastructure --startup-project API --context AppDbContext --idempotent -o docs/InitialCreate.sql
```

## اختبارات القيود على PostgreSQL فعلي

```powershell
# اختياري: تجاوز سلسلة الاتصال. بدونها تُشتق من API/appsettings*.json مع Database=esp32_security_test
$env:ESP32_SECURITY_TEST_DB = "Host=localhost;Port=5432;Database=esp32_security_test;Username=postgres;Password=<YOUR_PASSWORD>"
dotnet test API.IntegrationTests
```

لا يُستخدم `EnsureCreated` بديلاً عن Migrations. الترحيل `InitialCreate` يضم فهارس التعبير؛ الترحيل `UsersFullNameCheckAndSoftDeleteUniqueness` يضيف `ck_users_full_name_not_blank` ويقيّد تفرد username/email بالحسابات غير المؤرشفة (`deleted_at IS NULL`).
