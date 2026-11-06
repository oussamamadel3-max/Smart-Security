# قاموس حقول ASP.NET Core Identity

الحقول الموروثة من Identity **لا تُعاد تعريفها** في الكيانات فقط لإضافة تعليقات. هذا القاموس يوثّق معناها في سياق النظام وارتباطها بالتصميم المنطقي `users` / `roles` / `user_roles`.

## users ← `IdentityUser<Guid>` (`Domain.Entities.Auth.User`)

| عمود PostgreSQL | خاصية Identity | المعنى في النظام |
|-----------------|----------------|------------------|
| id | `Id` | المفتاح الأساسي للحساب (uuid). |
| username | `UserName` | اسم الدخول المعروض/المُدخل؛ يُفرض غير فارغ بعد trim بقيد CHECK؛ التفرد عبر `UserNameIndex` + `ux_users_username_lower` على الحسابات غير المؤرشفة فقط (`deleted_at IS NULL`). |
| normalized_user_name | `NormalizedUserName` | شكل موحّد للبحث والتفرد دون تمييز حالة الأحرف (يحقّق UNIQUE المنطقي لاسم الدخول للحسابات النشطة/غير المؤرشفة). |
| email | `Email` | البريد الاختياري؛ NULL يعني عدم التسجيل؛ التفرد الجزئي `ux_users_email_lower` حيث البريد موجود و`deleted_at IS NULL`. |
| normalized_email | `NormalizedEmail` | شكل موحّد للبريد للبحث (فهرس غير فريد `EmailIndex`). |
| email_confirmed | `EmailConfirmed` | هل أُكّد البريد عبر تدفق المصادقة. |
| password_hash | `PasswordHash` | تهشير كلمة المرور؛ لا تُخزَّن كلمة مرور صريحة. NULL إن لم يُعيَّن مزود كلمات مرور. |
| security_stamp | `SecurityStamp` | ختم يُبطِل الجلسات/الرموز عند تغيير حساس (تعطيل، تغيير كلمة المرور). |
| concurrency_stamp | `ConcurrencyStamp` | علامة تفاؤل لمنع الكتابة المتزامنة المتعارضة. |
| phone_number | `PhoneNumber` | رقم هاتف اختياري لإطار Identity (خارج متطلبات النسخة الأولى التشغيلية). |
| phone_number_confirmed | `PhoneNumberConfirmed` | تأكيد رقم الهاتف إن استُخدم. |
| two_factor_enabled | `TwoFactorEnabled` | تفعيل المصادقة الثنائية إن فُعّلت لاحقاً. |
| lockout_end | `LockoutEnd` | نهاية القفل المؤقت بعد فشل الدخول. |
| lockout_enabled | `LockoutEnabled` | هل يُسمح بقفل الحساب تلقائياً. |
| access_failed_count | `AccessFailedCount` | عداد محاولات الدخول الفاشلة. |

حقول مخصّصة موثّقة على الكيان بـ`<summary>` عربي: `full_name`, `is_active`, `created_at`, `updated_at`, `deleted_at`.

## roles ← `IdentityRole<Guid>` (`Domain.Entities.Auth.Role`)

| عمود | خاصية | المعنى |
|------|--------|--------|
| id | `Id` | مفتاح الدور (uuid). |
| code | `Name` (عمود مُعاد تسميته إلى `code`) | رمز الدور الثابت: `OWNER` / `ADMIN`؛ يُستخدم في تفويض Identity. |
| normalized_name | `NormalizedName` | شكل موحّد لرمز الدور. |
| concurrency_stamp | `ConcurrencyStamp` | تعارض الكتابة. |
| name | `DisplayName` | اسم العرض للمستخدم/الواجهة. |

## user_roles ← `IdentityUserRole<Guid>` (`Domain.Entities.Auth.UserRole`)

| عمود | خاصية | المعنى |
|------|--------|--------|
| user_id | `UserId` | الحساب الممنوح له الدور. |
| role_id | `RoleId` | الدور الممنوح. |

حقول مخصّصة على الكيان: `assigned_at`, `assigned_by_user_id`.

## جداول Identity المساعدة (خارج تفصيل نموذج الوثيقة)

| جدول | الغرض |
|------|--------|
| user_claims | مطالبات إضافية للمستخدم. |
| role_claims | مطالبات مرتبطة بالدور. |
| user_logins | تسجيلات دخول خارجية (OAuth…) إن لزم. |
| user_tokens | رموز Identity (استعادة، إلخ). |

لا تُنشأ جداول `users`/`roles` منطقية موازية — هذه هي النسخة الوحيدة للهوية وكلمات المرور.
