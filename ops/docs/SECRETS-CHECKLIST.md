# قائمة تحقق — لا سر في Git أو صورة أو سجل عام

تشغيل آلي: `../scripts/Test-SecretsHygiene.ps1`

## قبل كل دمج / صورة

- [ ] لا ملف `.env` متتبَّع
- [ ] لا `mosquitto/passwd` متتبَّع
- [ ] لا `*.key` / `*.pem` / `*.pfx` ضمن `ops/tls` متتبَّعة
- [ ] `Auth:Jwt:SigningKey` في `appsettings.json` فارغ أو غائب القيمة
- [ ] لا `Password=` حقيقي في ملفات `*.example`
- [ ] لا JWT (`eyJ…`) في تقارير `docs/reports`
- [ ] سجلات `docker compose logs` لا تُلصق في Git وفيها كلمات مرور
- [ ] Dockerfile/compose لا يمرّر `ARG` بأسرار إلى طبقات الصورة
- [ ] Firebase service account JSON غير موجود في المستودع

## بعد بناء صورة (عند وجودها لاحقاً)

- [ ] `docker history` / مسح طبقات بلا أسرار
- [ ] متغيرات التشغيل تُحقن وقت التشغيل لا البناء

## دليل OPS-T-02

سكربت النظافة يجب أن يخرج **exit 0** على المستودع الحالي قبل اعتبار بطاقة «لا سر في صورة/سجل» جاهزة للتوثيق.
