# مصفوفة تتبع قاعدة البيانات — نظام الأمان الذكي

المرجع: `تصميم_قاعدة_بيانات_نظام_الأمان_الذكي_v1.0.docx` (استخراج كامل: 18 جدولاً تشغيلياً + Identity، الفصلان 11 و12).

الحالة: **معتمد للتنفيذ** ما لم يُعلَّم بـ `معلق`.

أسماء الجداول الفعلية للهوية: `users` / `roles` / `user_roles` (منطق الوثيقة؛ دون نسخة ثانية بجانب AspNet*). جداول Identity المساعدة: `user_claims`, `user_logins`, `user_tokens`, `role_claims`.

| الجدول | الحقل / القيد / الفهرس | النوع / القاعدة | القرار | التهيئة | الترحيل |
|--------|-------------------------|-----------------|--------|---------|---------|
| users | id | uuid PK | معتمد → Identity `User.Id` | `UserConfiguration` | InitialCreate |
| users | username | varchar(120) NOT NULL؛ غير فارغ بعد trim؛ UNIQUE جزئي عبر `NormalizedUserName` + `lower(username)` حيث `deleted_at IS NULL` | معتمد | `UserConfiguration` + SQL | InitialCreate + UsersFullName… |
| users | email | varchar(254) NULL؛ UNIQUE جزئي `lower(email)` حيث email NOT NULL و`deleted_at IS NULL` | معتمد | SQL | InitialCreate + UsersFullName… |
| users | full_name | varchar(200) NOT NULL؛ غير فارغ بعد trim | معتمد | `UserConfiguration` | InitialCreate + UsersFullName… |
| users | password_hash | text (Identity) | معتمد — لا نسخة ثانية | Identity | InitialCreate |
| users | is_active | boolean NOT NULL default true | معتمد | `UserConfiguration` | InitialCreate |
| users | created_at, updated_at | timestamptz NOT NULL | معتمد | `UserConfiguration` | InitialCreate |
| users | deleted_at | timestamptz NULL؛ CHECK مع is_active | معتمد | SQL CHECK | InitialCreate |
| users | CHECK length(btrim(username))>0 | قيد DB | معتمد | SQL | InitialCreate |
| users | CHECK length(btrim(full_name))>0 | قيد DB | معتمد | SQL | UsersFullNameCheckAndSoftDeleteUniqueness |
| users | فهارس فريدة جزئية بعد soft-delete | إعادة استخدام username/email بعد الأرشفة | معتمد | SQL + `UserNameIndex` filter | UsersFullNameCheckAndSoftDeleteUniqueness |
| roles | id, code(=Name), name(=DisplayName) | uuid؛ code UNIQUE | معتمد | `RoleConfiguration` | InitialCreate |
| user_roles | (user_id, role_id) PK؛ assigned_at؛ assigned_by_user_id | مركّب + FK | معتمد | `UserRoleConfiguration` | InitialCreate |
| devices | جميع الأعمدة + UNIQUE(hardware_uid) + INDEX(lifecycle_status) + CHECK الحالات وretired_at | انظر الكيان | معتمد | `DeviceConfiguration` | InitialCreate |
| device_ownerships | UNIQUE(device_id,version)؛ UNIQUE(device_id,id)؛ UNIQUE جزئي مفتوح؛ CHECK status/version | ملكية مفتوحة واحدة | معتمد | `DeviceOwnershipConfiguration` + SQL | InitialCreate |
| device_credentials | auth_type CHECK + حقول مشروطة | معتمد | `DeviceCredentialConfiguration` | InitialCreate |
| device_sensors | UNIQUE(device_id,sensor_key)؛ UNIQUE(device_id,id)؛ CHECK type/diag | معتمد | `DeviceSensorConfiguration` | InitialCreate |
| device_settings | PK=device_id؛ alarm_rule jsonb object؛ مدد موجبة؛ بلا افتراض إنتاجي لـ event_buffer_capacity / command_ttl_seconds / معايرة SW-420 | **معلق رقمياً** للسعة وTTL والمعايرة | `DeviceSettingsConfiguration` | InitialCreate |
| device_states | 1:1؛ CHECK battery/sequence/enums | معتمد | `DeviceStateConfiguration` | InitialCreate |
| device_commands | UNIQUE(device_id,command_sequence)؛ UNIQUE جزئي PENDING؛ UNIQUE idempotency؛ FK مركّب ownership وalarm_event | معتمد | `DeviceCommandConfiguration` + SQL | InitialCreate |
| command_status_history | bigint identity؛ INDEX(command_id,changed_at,id) | معتمد | `CommandStatusHistoryConfiguration` | InitialCreate |
| alarm_events | UNIQUE(device_id,event_uid)؛ UNIQUE(device_id,id)؛ FK مركّب sensor/ownership | معتمد | `AlarmEventConfiguration` | InitialCreate |
| device_faults | UNIQUE(device_id,fault_uid)؛ FK مركّب | معتمد | `DeviceFaultConfiguration` | InitialCreate |
| event_loss_reports | UNIQUE(device_id,report_uid) | معتمد | `EventLossReportConfiguration` | InitialCreate |
| push_tokens | UNIQUE(token_fingerprint)؛ فهرس نشط per user | معتمد | `PushTokenConfiguration` | InitialCreate |
| admin_alerts | FKs users | معتمد | `AdminAlertConfiguration` | InitialCreate |
| notifications | مصدر واحد CHECK؛ تفرد جزئي لكل مصدر+مستلم | معتمد | `NotificationConfiguration` + SQL | InitialCreate |
| notification_deliveries | bigint identity؛ UNIQUE(notification,token,attempt_no) | معتمد | `NotificationDeliveryConfiguration` | InitialCreate |
| audit_logs | bigint identity؛ فهارس device/actor/action؛ Restrict | معتمد | `AuditLogConfiguration` | InitialCreate |
| outbox_messages | UNIQUE(message_type,aggregate_id)؛ فهرس غير المعالَج | معتمد | `OutboxMessageConfiguration` | InitialCreate |
| عام | ON DELETE: Restrict/NoAction — ممنوع CASCADE على الملكيات/الحوادث/الأوامر/التدقيق | معتمد | كل التهيئات | InitialCreate |
| §11 هوية مستقلة Guid | ربط واحد | معتمد | Identity + Guid | InitialCreate |
| §11 عتاد SW-420 / سعة / TTL | بلا بذور إنتاجية | **معلق** | لا seed | — |
| §11 ساعة الجهاز / outbox retries / الاحتفاظ | خارج مخطط الأعمدة الأساسي | **معلق تشغيلياً** | — | — |
| §12 تغطية FR | الجداول أعلاه تغطي FR-001…FR-036 على مستوى النموذج | معتمد للنموذج | — | — |

## قرارات معلقة (ليست قيماً معتمدة للإنتاج)

| البند | الحالة |
|-------|--------|
| مخطط معايرة/عتبة SW-420 داخل `alarm_rule` | معلق للاختبار الميداني |
| `event_buffer_capacity` قيمة رقمية | معلق؛ العمود موجود CHECK > 0 بلا DEFAULT إنتاجي |
| `command_ttl_seconds` قيمة رقمية | معلق؛ العمود موجود CHECK > 0 بلا DEFAULT إنتاجي |
| طراز ESP32 النهائي ودائرة 12V | خارج المخطط |
| آلية clock trust بعد إعادة التشغيل | عقد رسائل — خارج الأعمدة |
| عدد إعادة محاولة outbox وآلية الأرشفة الشهرية | تشغيلي — خارج الترحيل الأولي |
