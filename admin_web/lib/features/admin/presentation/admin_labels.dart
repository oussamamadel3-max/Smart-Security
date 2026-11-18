import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../domain/admin_ownership_view.dart';
import '../domain/entities/admin_device_entity.dart';

String adminOwnershipLabel(BuildContext context, AdminOwnershipKind kind) {
  final tr = context.tr;
  return switch (kind) {
    AdminOwnershipKind.pending => 'PENDING · ${tr.linkPendingTitle}',
    AdminOwnershipKind.activeAwaitingAck =>
      'ACTIVE · ${tr.awaitingOwnershipSync}',
    AdminOwnershipKind.active => 'ACTIVE · ${tr.ownershipActive}',
    AdminOwnershipKind.unlinked => 'غير مرتبط',
    AdminOwnershipKind.closed => 'مغلقة',
    AdminOwnershipKind.unknown => 'غير معروفة',
  };
}

String adminDeviceBanner(AdminDeviceEntity device) {
  return switch (device.ownershipKind) {
    AdminOwnershipKind.pending =>
      'ربط أولي بحالة PENDING. هذه ليست ملكية ACTIVE، ولا يُمنح تحكم تشغيلي من هنا.',
    AdminOwnershipKind.activeAwaitingAck =>
      'الملكية ACTIVE على الخادم، والتحكم ينتظر إقرار الجهاز لإصدار الملكية. لا زر تشغيل في لوحة الإدارة.',
    AdminOwnershipKind.active =>
      'الملكية ACTIVE وفق آخر قراءة من الخادم. لوحة الإدارة لا ترسل أوامر تشغيل.',
    AdminOwnershipKind.closed => 'الملكية السابقة مغلقة.',
    AdminOwnershipKind.unlinked => 'لا توجد ملكية مفتوحة لهذا الجهاز.',
    AdminOwnershipKind.unknown => 'حالة الملكية غير معروفة من الاستجابة.',
  };
}
