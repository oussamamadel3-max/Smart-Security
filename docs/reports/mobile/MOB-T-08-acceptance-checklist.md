# قائمة تحقق قبول الموبايل (MOB-T-08)

التاريخ: 2026-11-17 · الوضع: Autonomous Sequential (جهاز/iOS/G1 مؤجّلان)

| # | السيناريو | دليل آلي | ميداني |
|---|-----------|----------|--------|
| 1 | دخول OWNER + Access ذاكرة / Refresh آمن | auth_session + interceptor tests | NOT VERIFIED |
| 2 | Refresh single-flight لعدة 401 | auth_interceptor_test | NOT VERIFIED |
| 3 | 403 لا يجدّد الجلسة | auth_interceptor_test | NOT VERIFIED |
| 4 | قائمة PENDING محدودة بلا تحكم/سجل | owner_device_entity_test | NOT VERIFIED |
| 5 | ACTIVE بانتظار sync يظهر مؤشرات بلا نشر مبكر | entity + detail UI | NOT VERIFIED |
| 6 | أمر PENDING ≠ حالة مؤكدة | device_command_entity_test | NOT VERIFIED |
| 7 | TIMED_OUT يحظر المتعارض حتى المصالحة | device_command_entity_test | NOT VERIFIED |
| 8 | سجل أحداث: wasBuffered بلا مدة مختلقة | alarm_event_mapper_test | NOT VERIFIED |
| 9 | فتح إشعار قديم 403/404 بلا تفاصيل محلية | open_notification_usecase_test | NOT VERIFIED |
| 10 | Wi-Fi لا يدّعي نجاح بلا تأكيد جهاز | wifi_provisioning_test | NOT VERIFIED (عتاد) |
| 11 | رفض جهاز أجنبي / إسقاط cache | devices_repository_cache_test | NOT VERIFIED |
| 12 | FCM حي / هاتفان / نقل ملكية / شبكة ضعيفة | — | NOT VERIFIED |
| 13 | Wi-Fi محلي مع استمرار إنذار محلي | — | NOT VERIFIED (ESP) |
