String auditActionLabel(String action) {
  return switch (action) {
    'OWNER_CREATED' => 'إنشاء حساب',
    'OWNER_UPDATED' => 'تعديل حساب',
    'OWNER_DISABLED' => 'تعطيل حساب',
    'OWNER_ARCHIVED' => 'أرشفة حساب',
    'DEVICE_REGISTERED' => 'تسجيل جهاز',
    'DEVICE_LINKED' => 'ربط جهاز',
    'OWNERSHIP_ACTIVATED' => 'تفعيل ملكية',
    'OWNERSHIP_TRANSFERRED' => 'نقل ملكية',
    'OWNERSHIP_UNLINKED' => 'إلغاء ربط',
    'DEVICE_ARCHIVED' => 'أرشفة جهاز',
    'ADMIN_ALERT_CREATED' => 'تنبيه إداري',
    _ => action,
  };
}

String auditResultLabel(String result) {
  return switch (result) {
    'SUCCESS' => 'نجاح',
    'REJECTED' => 'مرفوض',
    'FAILED' => 'فشل',
    _ => result,
  };
}
