# خريطة مجلدات التخطيط — نظام الأمان الذكي

**تسلسل التنفيذ بين كل المشاريع:** [`EXECUTION_SEQUENCE.md`](./EXECUTION_SEQUENCE.md)

| المجلد | المسؤولية | ملفات التخطيط |
|--------|-----------|----------------|
| `security_backEnd` | باك إند .NET / API / عمال MQTT·Outbox·FCM / احتفاظ / NFR-008 خادم | `backend-development-plan.md` · `backend-development-tasks.md` · `backend-current.md` |
| `security_esp` | عتاد + firmware ESP32 / ESP-IDF / NFR-008 جهاز | `esp-development-plan.md` · `esp-development-tasks.md` · `esp-current.md` |
| `security_mobile` | تطبيق مالك Flutter + FCM عميل + Wi-Fi محلي | `mobile-development-plan.md` · `mobile-development-tasks.md` · `mobile-current.md` |
| `admin_web` | Flutter Web للمالك + لوحة الأدمن | `web-development-plan.md` · `web-development-tasks.md` · `web-current.md` |
| `integration` | عقد G1 + مصفوفة قبول + بوابة G2 + تعريف الإنجاز | `integration-development-plan.md` · `integration-development-tasks.md` · `integration-current.md` · `integration-acceptance-matrix.md` · `integration-definition-of-done.md` |
| `ops` | Compose/Mosquitto، بيئات، نشر، نسخ، مراقبة، ميداني | `ops-development-plan.md` · `ops-development-tasks.md` · `ops-current.md` |

## مراحل الخطة P0…P7 (مرجع تتبّع)

| مرحلة | المعنى | أين تُغلق غالباً |
|--------|--------|-------------------|
| **P0** | قرارات عتاد/عقود/بيئات | `security_esp` T-01 (G0) + `integration` مسودة |
| **P1** | تجربة عتاد محلية | `security_esp` T-02…T-04 |
| **P2** | DB + MQTT محلي | باك T-01 (DB) + `ops` T-01 |
| **P3** | شريحة متكاملة أولى | باك أوامر/حوادث + ESP MQTT + ويب قائمة/أوامر |
| **P4** | بقية وظائف SRS | باك/ESP/واجهات المتبقي |
| **P5** | واجهات نهائية | `admin_web` + `security_mobile` |
| **P6** | VPS وتجربة | `ops` T-02…T-06 |
| **P7** | قبول إنتاج | `integration` T-05 (G2) |

## إغلاق المهام

قبل تعليم أي مهمة `[x]`: اتبع `integration/integration-definition-of-done.md` (خطة §14).

تقدير المدة 12–18 أسبوعاً ومراجع تقنية §15 في وثيقة الخطة معلومات تخطيطية وليست مهام تنفيذ.

المرجع الأعلى: `SRS_نظام_الأمان_الذكي_v1.0.docx` و`خطة_تطوير_نظام_الأمان_الذكي_v1.0.docx`.
