import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Arabic labels taken from `design/index.html` owner views when shared l10n
/// has no equivalent short string. Web-only copy, not a new l10n catalog.
abstract final class OwnerVisualCopy {
  static const ownerCrumb = 'المالك';
  static const deviceDetailSubtitle =
      'متابعة حالات الجهاز وإرسال الأوامر عبر الخادم.';
  static const historySubtitle =
      'الحوادث والأوامر المسجلة ضمن الملكية الحالية، مع تمييز التأخر.';
  static const eventDetailSubtitle = 'عرض الحادثة بحسب صلاحية الملكية الحالية.';
  static const commandDetailSubtitle = 'حالة الأمر المؤكدة وبيانات الطلب.';
  static const controlTitle = 'التحكم بالجهاز';
  static const commandsHint =
      'الأوامر تُرسل إلى API وتظل معلقة حتى يؤكد الجهاز تنفيذها.';
  static const connectionTitle = 'حالة الاتصال';
  static const protectionTitle = 'حالة الحماية';
  static const sirenTitle = 'حالة الصفارة';
  static const hardwareTitle = 'سلامة العتاد';
  static const lastAckHint = 'آخر تأكيد من الجهاز';
  static const sirenHint = 'الكتم لا يعطّل الحماية';
  static const hardwareHint = 'لا تتوفر قراءة تشخيص مؤكدة';
  static const armedShort = 'مفعّلة';
  static const disarmedShort = 'معطّلة';
  static const unknownShort = 'غير معروفة';
  static const healthyShort = 'سليم';
  static const faultShort = 'عطل';
  static const sirenOffShort = 'متوقفة';
  static const sirenAlarmShort = 'إنذار';
  static const sirenTestShort = 'اختبار';
  static const executed = 'منفذ';
  static const pending = 'قيد التنفيذ';
  static const timedOut = 'انتهت المهلة';
  static const rejected = 'مرفوض';
  static const cancelled = 'ملغى';
  static const eventEnded = 'انتهت';
  static const eventActive = 'نشطة';
  static const details = 'التفاصيل';
  static const confirmedState = 'آخر حالة مؤكدة';
}

/// Breakpoints copied from `design/index.html` media queries.
abstract final class OwnerLayout {
  static const loginMaxWidth = 475.0;
  static const contentMaxWidth = 1650.0;
  static const topBarHeight = 78.0;

  static int deviceColumns(double width) => width > 780 ? 2 : 1;

  static int metricColumns(double width) => width > 1100 ? 4 : 2;

  static EdgeInsets pagePadding(double width) {
    final wide = width > 780;
    return EdgeInsets.fromLTRB(
      wide ? DesignTokens.spaceXxl : DesignTokens.spaceLg,
      wide ? DesignTokens.spaceXl : DesignTokens.spaceLg,
      wide ? DesignTokens.spaceXxl : DesignTokens.spaceLg,
      wide ? 60 : DesignTokens.spaceXl,
    );
  }
}

class OwnerBrandMark extends StatelessWidget {
  const OwnerBrandMark({super.key, this.size = 42});

  final double size;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(14),
        gradient: const LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [DesignTokens.brandMarkStart, DesignTokens.brandMarkEnd],
        ),
      ),
      child: Text(
        '♜',
        style: TextStyle(fontSize: size * 0.5, color: DesignTokens.white),
      ),
    );
  }
}

class OwnerPageHeader extends StatelessWidget {
  const OwnerPageHeader({
    super.key,
    required this.title,
    required this.subtitle,
    this.trailing,
    this.eyebrow,
    this.section = OwnerVisualCopy.ownerCrumb,
  });

  final String title;
  final String subtitle;
  final Widget? trailing;

  /// Full eyebrow. When set, l10n is not read.
  final String? eyebrow;

  /// Crumb section used when [eyebrow] is null.
  final String section;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final crumb = eyebrow ?? '${context.tr.appTitle} / $section';
    final copy = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          crumb,
          style: theme.textTheme.labelMedium?.copyWith(
            color: DesignTokens.muted,
          ),
        ),
        Text(
          title,
          style: theme.textTheme.headlineLarge?.copyWith(letterSpacing: -0.3),
        ),
        const SizedBox(height: DesignTokens.spaceXs),
        Text(
          subtitle,
          style: theme.textTheme.bodySmall?.copyWith(color: DesignTokens.muted),
        ),
      ],
    );
    return Padding(
      padding: const EdgeInsets.only(bottom: DesignTokens.spaceXl),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final stacked = trailing != null && constraints.maxWidth < 560;
          if (stacked) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                copy,
                const SizedBox(height: DesignTokens.spaceMd),
                trailing!,
              ],
            );
          }
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: copy),
              if (trailing != null) ...[
                const SizedBox(width: DesignTokens.spaceLg),
                trailing!,
              ],
            ],
          );
        },
      ),
    );
  }
}
