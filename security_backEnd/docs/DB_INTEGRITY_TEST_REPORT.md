# تقرير سلامة وتكامل قاعدة البيانات — نظام الأمان الذكي (ESP32)

**التاريخ:** 2026-11-03  
**البيئة:** PostgreSQL محلي معزول — `esp32_security_test` فقط  
**الترحيلات المطبَّقة:** `InitialCreate` + `UsersFullNameCheckAndSoftDeleteUniqueness`  
**نتيجة الاختبارات الآلية:** **37 / 37 ناجحة**

> ملاحظة نطاق: نص المهمة يذكر «فروع / خدمات / مرفقات مراجعة» وهو نموذج مختلف. هذا التقرير يغطّي **المخطط الفعلي** لنظام الأمان الذكي (أجهزة، ملكيات، أوامر، أحداث، إشعارات، تدقيق، outbox، Identity).

---

## 1) ما تمت مراجعته

| البند | العدد (من القاعدة الفعلية بعد الترحيل) |
|--------|----------------------------------------|
| جداول `public` | **25** (24 تصميم + `__EFMigrationsHistory`) |
| جداول التصميم المعتمدة | **24** |
| أعمدة | **226** |
| مفاتيح خارجية | **41** |
| قيود CHECK | **53** |
| فهارس | **91** |
| FK بـ `ON DELETE CASCADE` | **0** (الكل Restrict وفق السياسة) |

مصفوفة التتبع المرجعية: `docs/DB_TRACKING_MATRIX.md` + `docs/IDENTITY_FIELD_DICTIONARY.md`.

---

## 2) بيانات الاختبار (Seed) — أعداد عند الإنشاء

البذرة قابلة لإعادة الإنشاء بمعرّفات ثابتة في `API.IntegrationTests/Support/SeedIds.cs` عبر `DatabaseScenarioSeed`.

| الجدول | سجلات البذرة |
|--------|---------------|
| users | 4 (Admin, OwnerA, OwnerB, Archived) |
| roles | 2 (ADMIN, OWNER) |
| user_roles | 3 |
| devices | 2 |
| device_ownerships | 2 (منتهية + نشطة — سيناريو نقل) |
| device_credentials | 1 |
| device_sensors | 1 |
| device_settings | 2 |
| device_states | 2 |
| device_commands | 2 (EXECUTED + PENDING) |
| command_status_history | 3 |
| alarm_events | 1 |
| device_faults | 1 |
| event_loss_reports | 1 |
| push_tokens | 1 |
| admin_alerts | 1 |
| notifications | 3 (ALARM / FAULT / ADMIN_ALERT) |
| notification_deliveries | 1 |
| audit_logs | 3 |
| outbox_messages | 1 |
| user_claims / user_logins / user_tokens / role_claims | 0 (جداول Identity مساعدة؛ فارغة عمداً في هذه الجولة) |

اختبار نقل الملكية (`OwnershipTransfer_…`) يضيف جهازاً إضافياً بعد الـ seed داخل نفس قاعدة الاختبار؛ لذلك قد تظهر أعداد أعلى قليلاً بعد تشغيل المجموعة كاملة.

---

## 3) الاختبارات المنفَّذة ونتائجها

| المجموعة | الغرض | النتيجة |
|----------|--------|---------|
| `DatabaseSchemaReviewTests` (6) | بنية، FK Restrict، CHECK، فهارس soft-delete، الترحيلات | نجحت |
| `DatabaseSeedIntegrityTests` (8) | بذرة مترابطة، JOIN، أدوار، outbox، أرشفة | نجحت |
| `DatabaseConstraintTests` (20) | قيود إيجابية/سلبية مع Transaction+Rollback | نجحت |
| `DatabaseTransactionTests` (3) | Rollback، نقل ملكية، عدم تسرب بعد فشل قيد | نجحت |
| **المجموع** | | **37 ناجحة / 0 فاشلة** |

تشغيل:

```powershell
Remove-Item Env:ESP32_SECURITY_TEST_DB -ErrorAction SilentlyContinue
dotnet test API.IntegrationTests
```

إن وُجد `ESP32_SECURITY_TEST_DB` قديم بكلمة مرور خاطئة يتجاوز `appsettings` ويفشل الاتصال.

---

## 4) عينات القيود التي رُفضت بنجاح (سلبية)

- username / full_name فارغان بعد trim  
- تكرار username (case-insensitive) للحسابات غير المؤرشفة  
- إعادة استخدام username بعد soft-delete (مسموح)  
- `deleted_at` مع `is_active=true`  
- دورة حياة جهاز غير معتمدة  
- اعتماد جهاز بحقول لا تطابق `auth_type`  
- أمر PENDING ثانٍ لنفس الجهاز  
- `MUTE_ALARM` بلا `target_alarm_event_uid`  
- إشعار بمصدرين معاً  
- حذف مستخدم مرتبط بجهاز (Restrict)  
- FK لمعرف مستخدم غير موجود  
- بطارية خارج 0–100  
- `expires_at <= created_at`  
- نتيجة تدقيق غير معتمدة  

لم يُرصد تسرب لبيانات الاختبارات السلبية بعد Rollback.

---

## 5) قيود/علاقات ناقصة أو فجوات توثيقية

| البند | الحالة | ملاحظة |
|-------|--------|--------|
| فروع / خدمات / مرفقات اعتماد | خارج النطاق | غير موجودة في التصميم المعتمد لهذا النظام |
| بذرة `user_claims`/`user_logins`/`user_tokens`/`role_claims` | غير مملوءة | اختيارية لـ Identity؛ لا تؤثر على مسار التشغيل الأساسي الحالي |
| قواعد أعمال Backend (تفويض OWNER/ADMIN، قبول أمر من غير المالك) | غير مختبرة هنا | طبقة التطبيق — يجب اختبارها لاحقاً وظيفياً |
| قيم إنتاجية §11 (`event_buffer_capacity` / `command_ttl` / SW-420) | **معلقة** كما في المصفوفة | البذرة تستخدم قيماً اختبارية فقط |
| `docs/InitialCreate.sql` | يُفضَّل إعادة توليده بعد كل ترحيل | كان ناقصاً لترحيل soft-delete قبل إعادة التوليد |
| ضغط التزامن العالي (race على PENDING) | لم يُنفَّذ تحت حمل | الفهرس الجزئي موجود؛ اختبار الحمل خارج هذه الجولة |

---

## 6) اختلافات التصميم ↔ التنفيذ

- التنفيذ يطابق جداول الهوية عبر Identity (`users`/`roles`/`user_roles`) دون جداول موازية AspNet*.  
- التفرد بعد الأرشفة منفَّذ: `UserNameIndex` + `ux_users_username_lower` + `ux_users_email_lower` بشرط `deleted_at IS NULL`.  
- `ck_users_full_name_not_blank` منفَّذ.  
- لا CASCADE على علاقات الملكيات/الأحداث/الأوامر/التدقيق.

---

## 7) إصلاحات مقترحة (حسب الأولوية) — توثيق فقط (لم تُغيَّر البنية تلقائياً)

1. **P1 تشغيل:** إزالة/تصحيح أي `ESP32_SECURITY_TEST_DB` قديم في بيئة المطوّر قبل الاعتماد على نتائج الاختبار.  
2. **P2 توثيق:** الإبقاء على `docs/InitialCreate.sql` متزامناً مع كل ترحيل (`dotnet ef migrations script --idempotent`).  
3. **P3 تغطية:** إضافة اختبارات طبقة Application لقواعد التفويض ودورة الأوامر (ليست قيود DB).  
4. **P4 لاحق:** اختبار تزامن لسباق أمرين PENDING، وبذرة اختيارية لجداول Identity المساعدة إن لزم مسار OAuth/Claims.

---

## 8) اختبارات تعذّر تنفيذها / خارج النطاق

| الاختبار | السبب |
|----------|--------|
| فروع وخدمات ومراجعات اعتماد | ليست جزءاً من مخطط ESP32 |
| قاعدة الإنتاج | محظورة بالضوابط؛ استُخدمت `esp32_security_test` فقط |
| التحقق الوظيفي الكامل لـ API/MQTT | مرحلة لاحقة بعد سلامة القاعدة |
| حمل تزامني واسع | خارج نطاق هذه الجولة |

---

## 9) التقييم النهائي

**قاعدة البيانات جاهزة كأساس للنموذج والقيود** ضمن نطاق نظام الأمان الذكي: الجداول والعلاقات وCHECK والفهارس الحرجة وسلوك Restrict وتحقق soft-delete تم التحقق منها آلياً على PostgreSQL فعلي مع بذرة مترابطة واختبارات سلبية معزولة.

**ليست «جاهزة إنتاج تشغيلي كامل»** حتى تُغلق البنود المعلقة في §11 وتُكمَّل اختبارات قواعد الأعمال في Backend والاختبارات الوظيفية الشاملة.

**الحكم:** مقبول للانتقال إلى الاختبارات الوظيفية على مستوى التطبيق، مع متابعة توصيات P1–P3.
