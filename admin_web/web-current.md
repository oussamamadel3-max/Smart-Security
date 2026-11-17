# المهمة الحالية — الويب

status: waiting
task: WEB-T-01 مغلقة؛ WEB-T-02 لم تبدأ
srs: أساس واجهتي المالك والأدمن — T-01 مغلقة؛ FR-001 مؤجّل إلى T-02
parent: T-01 closed
branch: (محلي)
worktree: C:/Users/Osama/Desktop/SmartSecurity/admin_web

## السياق

- أُغلقت `WEB-T-01` في 2026-11-17 بموافقة `APPROVE TASK CLOSURE: WEB-T-01`.
- أمر المالك: **لا يبدأ تنفيذ WEB-T-02** مع تحليل `MOB-T-02`.

## تم في الإغلاق

- استهلاك `packages/smart_security_app_core` عبر path. لا اعتماد على `security_mobile`.
- حُذف `admin_web/app_core` بعد التحقق من عدم استيراده.
- إعادة التحقق 2026-11-17: `flutter analyze` بلا ملاحظات؛ `flutter test` **1/1**.
- بناء الويب السابق: `admin_web/build/web`. لم تُعد جلسة متصفح تفاعلية في جولة الإغلاق.
- لا Cookie/CSRF ولا رموز Auth في Web Storage.

## التالية

- `WEB-T-02` معلّقة حتى إذن تنفيذ صريح.
