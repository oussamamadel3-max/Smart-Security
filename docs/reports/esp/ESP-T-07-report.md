# تقرير مهمة — ESP-T-07 (تنفيذ الأوامر البعيدة والتأكيد والملكية)

التاريخ: 2026-11-18
الاستثناء المطبَّق: التنفيذ التسلسلي الذاتي لـESP — نطاق برمجي جزئي فقط (منطق صِرف).

## الملخص — منفَّذ (منطق صِرف، بلا نقل MQTT)

- `ESP-07` (تحقق الأمر + `OWNERSHIP_SYNC`): `remote_command.{h,c}` (ترتيب فحوص: تكرار `commandId` ← تطابق `ownershipVersion` ← حداثة `commandSequence` ← ثقة الساعة ← `expiresAt`) و`ownership_state.{h,c}` (`OWNERSHIP_SYNC`، `isLinked=false` يُحدَّث لا يُتجاهل).
- `ESP-08` (ACK والحالة الفعلية وعلامة ثقة الساعة): `command_ack_builder.{h,c}` يبني `COMMAND_ACK` يحمل **الحالة الفعلية الكاملة** (`armState`/`sirenState`/`healthState`/`currentAlarmEventUid`) + `resultStateSequence` + `confirmedOwnershipVersion` + `clockTrusted` — مطابق لحقول `docs/contracts/mqtt/v1/command-ack.schema.json` (مسودة، غير معتمدة G1).
- منع التنفيذ المكرر: `command_dedup.{h,c}` (حلقة بحجم 8 لمعرفات `commandId` المعاد إرسالها).
- رفض الأمر عند تعذر إثبات الوقت: فحص `clock_trusted` قبل `expiresAt` — مطابق لقرار ESP الثابت في `esp-context.md` (2026-11-16).

## ما لم يُنفَّذ عمداً (ليس نسياناً)

- **لا تحليل/بناء JSON فعلي** لـ`EXECUTE_COMMAND`/`COMMAND_ACK` — توجيه T-07 الصريح في البرومبت المرجعي: عند عدم اعتماد G1، تُستكمل المنطق الداخلي فقط وتبقى "أسماء الحقول الدقيقة وترتيب وصول STATE قبل/بعد ACK" رهناً بعقد G1.
- **لا نقل MQTT فعلي** — `T-06` محظورة بسبب غياب ESP-IDF.
- **لا اختبار وصول `STATE` قبل/بعد `ACK`** على عتاد أو وسيط حقيقي.

## حالة التنفيذ

**جزء من المنطق الداخلي: `SOFTWARE COMPLETE`. المهمة الأصلية: `BLOCKED BY EXTERNAL DEPENDENCY`** (G1 غير معتمد + لا ESP-IDF/MQTT). لا يُعلَّم `[x]`.

## الاختبار الفعلي المنفَّذ

`test_remote_command.c` (6)، `test_ownership_state.c` (3)، `test_command_ack_builder.c` (3)، `test_command_dedup.c` (3) — **15 اختباراً، 0 فشل**. يغطي: قبول أمر صالح، رفض تكرار `commandId` دون إعادة تنفيذ، رفض عدم تطابق الملكية حتى مع تسلسل حديث، رفض تسلسل قديم، رفض عند عدم ثقة الساعة بصرف النظر عن قيمة `expiresAt`، رفض أمر منتهي.

## اعتماديات متبقية

كل ما في §"ما لم يُنفَّذ عمداً" أعلاه، بالإضافة إلى اعتماد `G1` نفسه. مسجَّلة في `ESP-REMAINING-WORK.md`.

## 🧠 مقتطف هندسي من المهمة

ترتيب فحوص الرفض في `remote_command_verify` ليس اعتباطياً: فحص `ownershipVersion` يسبق فحص حداثة `commandSequence` عمداً، لأن تنفيذ أمر بملكية غير متطابقة يبقى خطأ سلامة حتى لو كان تسلسله "حديثاً" تقنياً — ترتيب الفحوص في أي دالة تحقق أمان يجب أن يعكس أولوية الخطر، لا الترتيب الأسهل للكتابة. يُطبَّق هذا في أي نقطة تحقق صلاحية متعددة الشروط (مصادقة + تفويض + صحة بيانات): تحقق الهوية/الملكية أولاً، تحقق محتوى الطلب بعدها. التحقق العملي: `test_ownership_mismatch_rejected_even_with_fresh_sequence` يثبت أن تسلسلاً حديثاً لا "يتجاوز" رفض الملكية.
