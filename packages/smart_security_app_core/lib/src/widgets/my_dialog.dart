import 'package:flutter/material.dart';
import '../errors/error_state.dart';
import '../theme/app_semantic_colors.dart';

// يوفر Dialog قابلة لإعادة الاستخدام مع:
// MyDialog
// showMyDialog()
// showErrorDialog()
// showMySheet()
// يدعم عدة أنماط: info, success, warning, danger, confirm, custom.
// هذا الملف يبني حوارات موحدة بألوان وأزرار متناسقة.

enum MyDialogType { info, success, warning, danger, confirm, custom }

enum MySheetSize { small, medium, large, full }

class _DialogConfig {
  const _DialogConfig({
    required this.color,
    required this.onColor,
    required this.icon,
    required this.confirmLabel,
    required this.showCancel,
    this.cancelLabel,
  });

  final Color color;
  final Color onColor;
  final IconData icon;
  final String confirmLabel;
  final bool showCancel;
  final String? cancelLabel;
}

Future<T?> showMyDialog<T>({
  required BuildContext context,
  required MyDialogType type,
  required String title,
  String? message,
  Widget? content,
  String? confirmLabel,
  String? cancelLabel,
  VoidCallback? onConfirm,
  VoidCallback? onCancel,
  bool barrierDismissible = true,
  bool showIcon = true,
  Widget? customIcon,
}) {
  return showDialog<T>(
    context: context,
    barrierDismissible: barrierDismissible,
    builder: (_) => MyDialog(
      type: type,
      title: title,
      message: message,
      content: content,
      confirmLabel: confirmLabel,
      cancelLabel: cancelLabel,
      onConfirm: onConfirm,
      onCancel: onCancel,
      showIcon: showIcon,
      customIcon: customIcon,
    ),
  );
}

Future<T?> showErrorDialog<T>(
  BuildContext context,
  ErrorState state, {
  String title = 'خطأ',
  String closeLabel = 'إغلاق',
}) {
  return showMyDialog<T>(
    context: context,
    type: state.requiresReauth ? MyDialogType.warning : MyDialogType.danger,
    title: title,
    message: state.message,
    confirmLabel: state.actionLabel ?? closeLabel,
    cancelLabel: state.actionLabel != null ? closeLabel : null,
    onConfirm: state.onAction,
  );
}

Future<T?> showMySheet<T>({
  required BuildContext context,
  required Widget child,
  String? title,
  MySheetSize size = MySheetSize.medium,
  bool isDismissible = true,
  bool showDragHandle = true,
  bool isScrollable = false,
  Color? backgroundColor,
}) {
  return showModalBottomSheet<T>(
    context: context,
    isDismissible: isDismissible,
    enableDrag: isDismissible,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (_) => MyBottomSheet(
      title: title,
      size: size,
      showDragHandle: showDragHandle,
      isScrollable: isScrollable,
      backgroundColor: backgroundColor,
      child: child,
    ),
  );
}

class MyDialog extends StatelessWidget {
  const MyDialog({
    super.key,
    required this.type,
    required this.title,
    this.message,
    this.content,
    this.confirmLabel,
    this.cancelLabel,
    this.onConfirm,
    this.onCancel,
    this.showIcon = true,
    this.customIcon,
  });

  final MyDialogType type;
  final String title;
  final String? message;
  final Widget? content;
  final String? confirmLabel;
  final String? cancelLabel;
  final VoidCallback? onConfirm;
  final VoidCallback? onCancel;
  final bool showIcon;
  final Widget? customIcon;

  static Future<bool?> show({
    required BuildContext context,
    required MyDialogType type,
    required String title,
    String? message,
    Widget? content,
    String? confirmLabel,
    String? cancelLabel,
    VoidCallback? onConfirm,
    VoidCallback? onCancel,
    bool barrierDismissible = true,
    bool showIcon = true,
    Widget? customIcon,
  }) {
    return showDialog<bool>(
      context: context,
      barrierDismissible: barrierDismissible,
      builder: (_) => MyDialog(
        type: type,
        title: title,
        message: message,
        content: content,
        confirmLabel: confirmLabel,
        cancelLabel: cancelLabel,
        showIcon: showIcon,
        customIcon: customIcon,
        onConfirm: onConfirm,
        onCancel: onCancel,
      ),
    );
  }

  _DialogConfig _resolveConfig(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final semantic = AppSemanticColors.of(context);

    return switch (type) {
      MyDialogType.info => _DialogConfig(
        color: scheme.primary,
        onColor: scheme.onPrimary,
        icon: Icons.info_outline_rounded,
        confirmLabel: confirmLabel ?? 'Ok',
        showCancel: cancelLabel != null,
        cancelLabel: cancelLabel,
      ),
      MyDialogType.success => _DialogConfig(
        color: semantic.success,
        onColor: semantic.onSuccess,
        icon: Icons.check_circle_outline_rounded,
        confirmLabel: confirmLabel ?? 'Done',
        showCancel: cancelLabel != null,
        cancelLabel: cancelLabel,
      ),
      MyDialogType.warning => _DialogConfig(
        color: semantic.warning,
        onColor: semantic.onWarning,
        icon: Icons.warning_amber_rounded,
        confirmLabel: confirmLabel ?? 'Co',
        showCancel: true,
        cancelLabel: cancelLabel ?? 'إلغاء',
      ),
      MyDialogType.danger => _DialogConfig(
        color: scheme.error,
        onColor: scheme.onError,
        icon: Icons.error_outline_rounded,
        confirmLabel: confirmLabel ?? 'تأكيد',
        showCancel: true,
        cancelLabel: cancelLabel ?? 'إلغاء',
      ),
      MyDialogType.confirm => _DialogConfig(
        color: scheme.primary,
        onColor: scheme.onPrimary,
        icon: Icons.help_outline_rounded,
        confirmLabel: confirmLabel ?? 'تأكيد',
        showCancel: true,
        cancelLabel: cancelLabel ?? 'إلغاء',
      ),
      MyDialogType.custom => _DialogConfig(
        color: scheme.primary,
        onColor: scheme.onPrimary,
        icon: Icons.widgets_outlined,
        confirmLabel: confirmLabel ?? 'حسنا',
        showCancel: cancelLabel != null,
        cancelLabel: cancelLabel,
      ),
    };
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final textTheme = theme.textTheme;
    final config = _resolveConfig(context);

    return Dialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 28, vertical: 40),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420),
        child: Padding(
          padding: const EdgeInsetsDirectional.fromSTEB(24, 24, 24, 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (showIcon) ...[
                DecoratedBox(
                  decoration: BoxDecoration(
                    color: config.color.withValues(alpha: 0.12),
                    shape: BoxShape.circle,
                  ),
                  child: SizedBox.square(
                    dimension: 64,
                    child: Center(
                      child:
                          customIcon ??
                          Icon(config.icon, color: config.color, size: 32),
                    ),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              Text(
                title,
                textAlign: TextAlign.center,
                style: textTheme.titleLarge?.copyWith(
                  color: scheme.onSurface,
                  fontWeight: FontWeight.w700,
                ),
              ),
              if (message != null) ...[
                const SizedBox(height: 8),
                Text(
                  message!,
                  textAlign: TextAlign.center,
                  style: textTheme.bodyMedium?.copyWith(
                    color: scheme.onSurfaceVariant,
                  ),
                ),
              ],
              if (content != null) ...[
                const SizedBox(height: 16),
                Flexible(child: SingleChildScrollView(child: content!)),
              ],
              const SizedBox(height: 24),
              Row(
                children: [
                  if (config.showCancel) ...[
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () {
                          Navigator.of(context).maybePop(false);
                          onCancel?.call();
                        },
                        child: Text(config.cancelLabel ?? 'إلغاء'),
                      ),
                    ),
                    const SizedBox(width: 12),
                  ],
                  Expanded(
                    child: ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: config.color,
                        foregroundColor: config.onColor,
                      ),
                      onPressed: () {
                        Navigator.of(context).pop(true);
                        onConfirm?.call();
                      },
                      child: Text(config.confirmLabel),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class MyBottomSheet extends StatelessWidget {
  const MyBottomSheet({
    super.key,
    required this.child,
    this.title,
    this.size = MySheetSize.medium,
    this.showDragHandle = true,
    this.isScrollable = false,
    this.backgroundColor,
  });

  final Widget child;
  final String? title;
  final MySheetSize size;
  final bool showDragHandle;
  final bool isScrollable;
  final Color? backgroundColor;

  double _heightFactor() {
    return switch (size) {
      MySheetSize.small => 0.32,
      MySheetSize.medium => 0.52,
      MySheetSize.large => 0.76,
      MySheetSize.full => 0.95,
    };
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final maxHeight = MediaQuery.sizeOf(context).height * _heightFactor();

    final body = Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (showDragHandle)
          Padding(
            padding: const EdgeInsetsDirectional.only(top: 10, bottom: 8),
            child: Container(
              width: 42,
              height: 4,
              decoration: BoxDecoration(
                color: scheme.outlineVariant,
                borderRadius: BorderRadius.circular(999),
              ),
            ),
          ),
        if (title != null)
          Padding(
            padding: const EdgeInsetsDirectional.fromSTEB(20, 8, 20, 12),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    title!,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                IconButton(
                  tooltip: MaterialLocalizations.of(context).closeButtonTooltip,
                  icon: const Icon(Icons.close_rounded),
                  onPressed: () => Navigator.of(context).maybePop(),
                ),
              ],
            ),
          ),
        Flexible(
          child: isScrollable
              ? SingleChildScrollView(
                  padding: const EdgeInsetsDirectional.fromSTEB(20, 0, 20, 24),
                  child: child,
                )
              : Padding(
                  padding: const EdgeInsetsDirectional.fromSTEB(20, 0, 20, 24),
                  child: child,
                ),
        ),
      ],
    );

    return SafeArea(
      child: Align(
        alignment: AlignmentDirectional.bottomCenter,
        child: ConstrainedBox(
          constraints: BoxConstraints(maxHeight: maxHeight),
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: backgroundColor ?? scheme.surface,
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(24),
              ),
            ),
            child: body,
          ),
        ),
      ),
    );
  }
}
