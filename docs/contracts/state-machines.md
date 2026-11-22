# آلات الحالات — مسودة مستخرجة من المرجع والكود

**الوصي:** Integration · **الحالة:** `DRAFT ACCEPTED` (INT-T-01 مغلقة 2026-11-22؛ G1 BLOCKED)  
**توحيد:** [`../../integration/contracts/MQTT-V1-DRAFT.md`](../../integration/contracts/MQTT-V1-DRAFT.md)

هذه الوثيقة تصف السلوك المعتمد في SRS وخطة التطوير §4.3 كما ينفذه الخادم اليوم. ليست محضر اعتماد G1.

مصدر الحقيقة للحماية والصفارة والتشخيص هو آخر حالة أكدها الجهاز عبر `STATE` أو `COMMAND_ACK` عندما يكون التسلسل أحدث. نجاح HTTP وMQTT PUBACK لا يغيران هذه الحالة.

**امتداد INT-T-02 (D-09):** ترتيب الوصول وقواعد تحديث `device_states` وإغلاق الأمر مفصّلة في [`../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md`](../../integration/contracts/D-09-COMMAND-ACK-AND-STATE.md) مع أمثلة JSON تحت `docs/contracts/mqtt/v1/samples/ordering/`. ليست اعتماد G1.

## الاتصال

- القيم المحسوبة: `ONLINE` / `OFFLINE` من `last_seen_at` ومهلة النبضة. لا يوجد عمود `is_online`.
- أي رسالة واردة موثّقة الهوية تحدّث `last_seen_at`، بما فيها `HEARTBEAT`.
- `HEARTBEAT` لا يغيّر `armState` ولا `sirenState` ولا `healthState` ولا `stateSequence`.
- مهلة النبضة في الكود: `ApprovedDeviceSettings.HeartbeatTimeoutSeconds = 3600`. هذه قيمة قائمة وقرار `D-03` الإنتاجي ما زال `OPEN`.

## الحماية

- `ARMED` | `DISARMED` | `UNKNOWN`.
- تُحدَّث فقط عندما يكون `stateSequence` أو `resultStateSequence` أكبر من التسلسل المخزن.
- التسلسل المساوي أو الأقدم يُتجاهل (`IGNORED_STALE_SEQUENCE`) ولا يرجّع الحالة.
- عند الانقطاع تبقى آخر حالة معروفة. الاتصال ليس دليلاً على الحماية.
- استمرار التسلسل عبر إعادة تشغيل ESP32 غير مثبت على عتاد. مبدأ `D-05` / **`G1-CD-7`** معتمد؛ الإثبات العتادي `OPEN`.
- **S1 (مالك 2026-11-21):** إقلاع طبيعي مع تخزين سليم → استعادة آخر إعداد حماية موثوق. عند تلف/تعذر الاستعادة → غير مسلّح محلياً بلا تسلح تلقائي؛ إعلان عطل/عدم يقين؛ لا عرض «محمي مؤكد»؛ العودة الطبيعية بعد تدخل مالك. فصل الحالة التشغيلية عن المعرفة المعروضة. التمثيل المسموح دون حقول جديدة: `DISARMED`/`UNKNOWN` + `healthState` FAULT/UNKNOWN و/أو `DEVICE_FAULT`. المرجع: `security_esp/docs/HARDWARE-DECISIONS-D01-D07.md` §S1.

## الصفارة

- `OFF` | `ALARM` | `TEST` | `UNKNOWN`.
- `MUTE_ALARM` لا ينهي الحادثة ولا يغيّر الحماية. `currentAlarmEventUid` يبقى حتى تقرير جهاز لاحق.
- `sirenState=OFF` لا يعني انتهاء الحادثة.
- نهاية الحادثة: `ALARM_EVENT` بنفس `eventUid` و`alarmPhase=ENDED` عندما يعود الحساس. غياب `alarmPhase` يعني `ACTIVE`. صف `ENDED` لا يعود `ACTIVE` برسالة قديمة. `EVENT_PERSISTED` يحمل `alarmPhase` للمرحلة التي أُقرت. إقرار قديم بلا مرحلة لا يحذف نهاية محفوظة.
- الخادم يرفض `TEST_SIREN` أثناء إنذار جارٍ (`TEST_DURING_ALARM`) و`STOP_TEST` إذا لم يكن الاختبار نشطاً (`STOP_TEST_NOT_ACTIVE`). هذا رفض طلب HTTP، وليس تنفيذاً مؤكداً على العتاد.

## سلامة العتاد

- `HEALTHY` | `FAULT` | `UNKNOWN`.
- لا تُكتب `HEALTHY` إلا إذا أرسلها الجهاز في اللقطة المقبولة.

## الأمر

حالات الخادم: `PENDING` | `EXECUTED` | `REJECTED` | `TIMED_OUT` | `CANCELLED`.

- `POST` ينشئ `PENDING` ويعيد `202`. `actualStateChanged` في الرد يبقى `false`.
- النشر إلى الوسيط يضبط `SentAt` ويبقي الحالة `PENDING`.
- `COMMAND_ACK` بحالة `EXECUTED` أو `REJECTED` ينقل الأمر ويسجل `command_status_history`.
- `TIMED_OUT` نتيجة مهلة إقرار، وليست إثبات فشل تنفيذ. التعارض مع أمر جديد يبقى حتى المصالحة.
- ACK بعد `TIMED_OUT` إقرار متأخر (`LATE_ACK`): يغلق الأمر بالنتيجة الواردة ويضبط `ReconciledAt`. لا يحيي أمراً `CANCELLED`.
- `STATE` أحدث من أمر `TIMED_OUT` غير مصالح يضبط `ReconciledAt` بسبب `RECONCILED_BY_STATE` ولا يعلّم الأمر `EXECUTED`.
- نقل الملكية يلغي الأوامر المعلقة للقديم (`CANCELLED` / `OWNERSHIP_TRANSFERRED`).
- ACK مكرر لأمر `EXECUTED` أو `REJECTED` لا يعيد الفتح.
- قيم المهلة الإنتاجية غير مجمّدة. `ApprovedDeviceSettings.CommandTimeoutSeconds = 10` قيمة كود، و`CommandTtlSeconds` يُمرَّر عند التسجيل. كلاهما `OPEN` بالنسبة إلى G1.

## الملكية

- `PENDING` | `ACTIVE` | `ENDED` | `CANCELLED`.
- الربط الأولي `PENDING` حتى يقر الجهاز `confirmedOwnershipVersion` المطابق لإصدار الملكية المفتوحة، فيصبح `ACTIVE`. الإقرار لا يسلّح الجهاز.
- النقل: ملكية الجديد `ACTIVE` داخل معاملة الخادم، والقديم `ENDED` فوراً على API. نشر أوامر الجديد يبقى محتجزاً حتى إقرار إصدار الملكية.
- `OWNERSHIP_SYNC` يحمل `ownershipVersion` و`isLinked` فقط، بلا هوية مالك. إلغاء الربط يرسل `isLinked=false`.
- الجهاز يقر الإصدار في `STATE` عبر `confirmedOwnershipVersion`، بما فيه `isLinked=false`. حقل الإقرار ليس داخل رسالة `OWNERSHIP_SYNC` نفسها.
