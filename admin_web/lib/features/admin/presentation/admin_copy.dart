/// Arabic copy for the admin console. Shared l10n stays untouched.
abstract final class AdminCopy {
  static const shellLabel = 'إدارة النظام';
  static const eyebrow = 'نظام الأمان الذكي / الإدارة';
  static const homeCrumb = 'الرئيسية';
  static const noOwnerCommands =
      'حساب المدير للمتابعة والإدارة. لا أدوات تحكم تشغيلي بأجهزة الملاك.';
  static const serverConfirms =
      'تُنفَّذ العملية على الخادم فقط، وتُعرض النتيجة بعد قبول الاستجابة.';
  static const roleOwnerPill = 'مالك';
  static const detailsLink = 'التفاصيل ←';
  static const viewLink = 'عرض ←';
  static const viewAccounts = 'عرض الحسابات ←';
  static const viewDevices = 'عرض الأجهزة ←';
  static const allStatuses = 'كل الحالات';
  static const filterActive = 'نشط';
  static const filterDisabled = 'معطّل';
  static const filterArchived = 'مؤرشف';
  static const filterOnline = 'متصل';
  static const filterOffline = 'غير متصل';
  static const noMatches = 'لا توجد نتائج مطابقة.';
  static const processPreview = 'معاينة مسار العملية';
  static const stepRequest = 'تسجيل الطلب';
  static const stepRequestHint = 'المعاملة';
  static const stepPending = 'بانتظار إقرار الجهاز';
  static const stepPendingHint = 'PENDING';
  static const stepOutcome = 'الملكية الجديدة';
  static const stepOutcomeHint = 'ACTIVE / ENDED';
  static const sensor = 'الحساس';
  static const sensorValue = 'اهتزاز';
  static const dashboard = 'لوحة الإدارة';
  static const accounts = 'إدارة الحسابات';
  static const addAccount = 'إضافة حساب';
  static const devices = 'إدارة الأجهزة';
  static const registerDevice = 'تسجيل جهاز';
  static const accountDetail = 'تفاصيل الحساب';
  static const deviceDetail = 'تفاصيل الجهاز الإدارية';
  static const link = 'ربط الجهاز بمالك';
  static const transfer = 'نقل ملكية الجهاز';
  static const unlink = 'إلغاء الربط';
  static const archive = 'أرشفة';
  static const save = 'حفظ';
  static const busy = 'جارٍ التنفيذ';
  static const cancel = 'إلغاء';
  static const discardTitle = 'مغادرة النموذج؟';
  static const discardBody = 'الحقول التي أدخلتها ستُمسح إذا غادرت هذه الصفحة.';
  static const edit = 'تعديل البيانات';
  static const disable = 'تعطيل الحساب';
  static const archiveAccount = 'أرشفة الحساب';
  static const refresh = 'تحديث';
  static const searchHint = 'بحث بالاسم أو البريد أو المعرف';
  static const emptyAccounts = 'لا توجد حسابات مالك.';
  static const emptyDevices = 'لا توجد أجهزة مسجلة.';
  static const back = 'رجوع';
  static const confirm = 'تأكيد';
  static const userName = 'اسم المستخدم';
  static const fullName = 'الاسم الكامل';
  static const email = 'البريد الإلكتروني';
  static const password = 'كلمة المرور';
  static const roleOwner = 'الدور: OWNER';
  static const status = 'الحالة';
  static const createdAt = 'تاريخ الإنشاء';
  static const active = 'نشط';
  static const disabled = 'معطّل';
  static const archived = 'مؤرشف';
  static const noEmail = 'بدون بريد';
  static const permissionNote =
      'إنشاء الحساب وتعديله وتعطيله وأرشفته تتطلب ADMIN وUsers.Manage على الخادم. رفض 403 لا يُعرض كنجاح.';
  static const disableConfirm =
      'تعطيل الحساب يبطل جلساته على الخادم. لن تُعتبر العملية ناجحة إلا إذا قبلها الخادم.';
  static const archiveConfirm =
      'الأرشفة تُرفض إذا بقي للجهاز ارتباط مفتوح. الخادم هو من يقرر التعارض.';
  static const saved = 'تم قبول العملية من الخادم.';
  static const dashboardSubtitle =
      'متابعة حسابات المالك والأجهزة المسجلة. بلا أوامر تشغيل.';
  static const accountsSubtitle =
      'إنشاء وتعديل وتعطيل وأرشفة حسابات المالك عبر API الإدارة.';
  static const devicesSubtitle =
      'عرض الاتصال والحماية والتشخيص والملكية. بلا أزرار تسليح أو كتم.';
  static const registerSubtitle =
      'أدخل هوية الجهاز والاعتماد والإعدادات المعتمدة. لا قيم إنتاجية افتراضية لـ D-03.';
  static const d03Notice =
      'سعة مخزن الأحداث ومدة صلاحية الأمر يجب أن تكونا موجبتين ومعتمدتين لهذا الجهاز. هذه الشاشة لا تخمّن مهلة أو سعة إنتاجية.';
  static const alarmRule = 'قاعدة الإنذار (كائن JSON معتمد)';
  static const hardwareUid = 'المعرف الفريد للجهاز';
  static const displayName = 'اسم الجهاز';
  static const authType = 'نوع اعتماد MQTT';
  static const credential = 'قيمة الاعتماد';
  static const buffer = 'سعة مخزن الأحداث';
  static const ttl = 'مدة صلاحية الأمر بالثواني';
  static const pendingBanner =
      'ربط أولي بحالة PENDING. هذه ليست ملكية ACTIVE، ولا يُمنح تحكم تشغيلي من هنا.';
  static const activeAwaitingBanner =
      'الملكية ACTIVE على الخادم، والتحكم ينتظر إقرار الجهاز لإصدار الملكية. لا زر تشغيل في لوحة الإدارة.';
  static const activeBanner =
      'الملكية ACTIVE وفق آخر قراءة من الخادم. لوحة الإدارة لا ترسل أوامر تشغيل.';
  static const unlinkedBanner = 'لا توجد ملكية مفتوحة لهذا الجهاز.';
  static const syncQueued =
      'أُدرج طلب مزامنة الملكية في الطابور. ذلك ليس إقراراً من الجهاز.';
  static const linkSubtitle =
      'الربط الأولي ينشئ ملكية PENDING. النقل مسار مختلف ويمنح ACTIVE على الخادم.';
  static const transferSubtitle =
      'بعد قبول الخادم تصبح الملكية ACTIVE للمالك الجديد، ويبقى نشر الأوامر حتى إقرار الجهاز.';
  static const owner = 'المالك';
  static const newOwner = 'المالك الجديد';
  static const selectOwner = 'اختر مالكاً نشطاً';
  static const noActiveOwners = 'لا يوجد مالك نشط غير مؤرشف.';
  static const connection = 'الاتصال';
  static const arm = 'الحماية';
  static const siren = 'الصفارة';
  static const health = 'التشخيص';
  static const lastSeen = 'آخر ظهور';
  static const ownership = 'الملكية';
  static const version = 'إصدار الملكية';
  static const confirmedVersion = 'الإصدار المؤكد';
  static const lifecycle = 'دورة الحياة';
  static const offlineNote =
      'انقطاع الاتصال لا يعني تعطيل الحماية ولا عطلاً مؤكداً.';
  static const auditLogs = 'السجلات الإدارية';
  static const alerts = 'التنبيهات الإدارية';
  static const auditDetail = 'تفاصيل سجل الإدارة';
  static const alertDetail = 'تفاصيل التنبيه الإداري';
  static const createAlert = 'إنشاء تنبيه إداري';
  static const openSavedAlert = 'فتح التنبيه المحفوظ';
  static const auditSubtitle =
      'قراءة سجل عمليات الإدارة من الخادم. المعرفات تُعرض كما أعادها API.';
  static const alertsSubtitle =
      'حفظ تنبيه لمالك عبر الخادم، ثم عرض نتيجة محاولة التسليم كما أعادها.';
  static const auditNotice =
      'الشاشة للقراءة فقط. نقل الملكية وأرشفة الجهاز يظهران بمعرف الهدف ومعرف الجهاز عندما يعيدهما الخادم.';
  static const alertNotice =
      'المتصفح لا يرسل إشعاراً ولا يحفظ أسراراً. قبول الحفظ ليس محاولة تسليم وليس نجاحاً وليس قراءة.';
  static const auditSearchHint = 'بحث في العملية أو النوع أو التفاصيل';
  static const auditFilters = 'تصفية وفق معاملات الخادم';
  static const applyFilters = 'تطبيق';
  static const auditAction = 'العملية';
  static const targetType = 'نوع الهدف';
  static const targetId = 'معرف الهدف';
  static const deviceId = 'معرف الجهاز';
  static const actor = 'المنفّذ';
  static const result = 'النتيجة';
  static const from = 'من (وقت ISO)';
  static const to = 'إلى (وقت ISO)';
  static const occurredAt = 'التوقيت';
  static const logId = 'السجل';
  static const details = 'التفاصيل';
  static const emptyAudit = 'لا توجد سجلات مطابقة.';
  static const emptyAlerts = 'لا توجد تنبيهات إدارية.';
  static const auditMissing = 'السجل غير متاح في هذه الجلسة.';
  static const auditMissingHint =
      'لا يوجد مسار جلب بالمعرف. افتح السجل من الصفحة الحالية للقائمة.';
  static const page = 'الصفحة';
  static const total = 'الإجمالي';
  static const previous = 'السابق';
  static const next = 'التالي';
  static const recipient = 'المستلم';
  static const recipientFilterHint = 'تصفية بمعرف المستلم (اختياري)';
  static const alertTitle = 'العنوان';
  static const alertBody = 'النص';
  static const requestLabel = 'حفظ الطلب';
  static const attemptLabel = 'محاولة التسليم';
  static const deliveryLabel = 'نتيجة المحاولة';
  static const readLabel = 'القراءة';
  static const alertId = 'معرف التنبيه';
  static const notificationStatus = 'حالة الإشعار المخزنة';
  static const auditPending =
      'سجل الإدارة والتنبيه الإداري يعرضان استجابة الخادم فقط. نجاح الحفظ ليس تسليماً، ولا تُعرض قراءة بلا دليل.';
  static const unlinkConfirm =
      'إلغاء الربط ينهي الملكية المفتوحة على الخادم دون تعيين مالك جديد.';
  static const archiveDeviceConfirm =
      'أرشفة الجهاز تجعله RETIRED وتبطل اعتماد الوصول. لا تُعرض الأسرار.';
  static const sameOwner = 'اختر مالكاً مختلفاً عن المالك الحالي.';
  static const open = 'عرض';
  static const devicesCount = 'الأجهزة';
  static const accountsCount = 'الحسابات';
  static const offlineCount = 'غير المتصلة';
  static const pendingCount = 'ربط معلّق';
  static const accountsHint = 'حسابات المالك';
  static const devicesHint = 'الأجهزة المسجلة';
  static const offlineHint = 'تحتاج متابعة';
  static const pendingHint = 'ربط لم يكتمل';

  static String fieldMessage(String field) {
    return switch (field) {
      'userName' => 'اسم المستخدم مطلوب وبحد أقصى 120 حرفاً.',
      'fullName' => 'الاسم الكامل مطلوب وبحد أقصى 200 حرف.',
      'password' => 'كلمة المرور يجب ألا تقل عن 8 أحرف.',
      'email' => 'البريد غير صالح.',
      'hardwareUid' => 'معرف الجهاز مطلوب وبحد أقصى 128 حرفاً.',
      'displayName' => 'اسم الجهاز مطلوب وبحد أقصى 150 حرفاً.',
      'authType' => 'نوع الاعتماد غير مدعوم.',
      'credential' => 'قيمة الاعتماد غير مقبولة لهذا النوع.',
      'alarmRule' => 'قاعدة الإنذار يجب أن تكون كائن JSON.',
      'eventBufferCapacity' => 'سعة المخزن يجب أن تكون عدداً موجباً معتمداً.',
      'commandTtlSeconds' =>
        'مدة صلاحية الأمر يجب أن تكون عدداً موجباً معتمداً.',
      'recipientUserId' => 'معرف المستلم يجب أن يكون GUID لحساب مالك.',
      'title' => 'العنوان مطلوب وبحد أقصى 160 حرفاً.',
      'body' => 'النص مطلوب وبحد أقصى 4000 حرف.',
      'guid' => 'المعرف يجب أن يكون GUID أو يُترك فارغاً.',
      'instant' => 'الوقت يجب أن يكون بصيغة ISO-8601 أو يُترك فارغاً.',
      _ => 'البيانات غير مقبولة.',
    };
  }

  static String accountStatus(String code) {
    return switch (code) {
      'ARCHIVED' => archived,
      'DISABLED' => disabled,
      _ => active,
    };
  }
}
