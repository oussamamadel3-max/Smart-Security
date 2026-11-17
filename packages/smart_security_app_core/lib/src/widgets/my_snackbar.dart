import 'package:flutter/material.dart';
import '../errors/error_state.dart';
import '../theme/app_semantic_colors.dart';

// يوفر واجهات سهلة لعرض Snackbar و Toast.
// يدعم أنواع مختلفة: info, success, warning, error, neutral.
// showErrorSnackBar() يترجم ErrorState إلى Snackbar تلقائيًا.
// مناسب لعرض أخطاء الشبكة والرسائل القصيرة.

enum MySnackType { info, success, warning, error, neutral }

enum MySnackPosition { bottom, top }

class _SnackConfig {
  const _SnackConfig({
    required this.background,
    required this.foreground,
    required this.icon,
  });

  final Color background;
  final Color foreground;
  final IconData icon;
}

_SnackConfig _resolveSnackConfig(
  MySnackType type,
  ColorScheme scheme,
  AppSemanticColors semantic,
) {
  return switch (type) {
    MySnackType.info => _SnackConfig(
      background: scheme.primary,
      foreground: scheme.onPrimary,
      icon: Icons.info_outline_rounded,
    ),
    MySnackType.success => _SnackConfig(
      background: semantic.success,
      foreground: semantic.onSuccess,
      icon: Icons.check_circle_outline_rounded,
    ),
    MySnackType.warning => _SnackConfig(
      background: semantic.warning,
      foreground: semantic.onWarning,
      icon: Icons.warning_amber_rounded,
    ),
    MySnackType.error => _SnackConfig(
      background: scheme.error,
      foreground: scheme.onError,
      icon: Icons.error_outline_rounded,
    ),
    MySnackType.neutral => _SnackConfig(
      background: scheme.inverseSurface,
      foreground: scheme.onInverseSurface,
      icon: Icons.info_outline_rounded,
    ),
  };
}

MySnackType _typeFromErrorState(ErrorState state) {
  return switch (state.displayType) {
    ErrorDisplayType.inline => MySnackType.warning,
    ErrorDisplayType.dialog => MySnackType.error,
    ErrorDisplayType.fullScreen => MySnackType.error,
    ErrorDisplayType.snackbar =>
      state.canRetry ? MySnackType.warning : MySnackType.error,
  };
}

void showErrorSnackBar(
  BuildContext context,
  ErrorState state, {
  MySnackPosition position = MySnackPosition.bottom,
  Duration duration = const Duration(seconds: 4),
}) {
  showMySnackBar(
    context,
    message: state.message,
    type: _typeFromErrorState(state),
    position: position,
    actionLabel: state.actionLabel,
    onAction: state.onAction,
    duration: duration,
  );
}

void showMySnackBar(
  BuildContext context, {
  required String message,
  MySnackType type = MySnackType.neutral,
  MySnackPosition position = MySnackPosition.bottom,
  String? actionLabel,
  VoidCallback? onAction,
  Duration duration = const Duration(seconds: 3),
  bool showIcon = true,
  bool dismissible = true,
}) {
  final theme = Theme.of(context);
  final config = _resolveSnackConfig(
    type,
    theme.colorScheme,
    AppSemanticColors.of(context),
  );

  final snackBar = SnackBar(
    behavior: SnackBarBehavior.floating,
    margin: EdgeInsetsDirectional.only(
      start: 16,
      end: 16,
      bottom: position == MySnackPosition.bottom ? 16 : 0,
      top: position == MySnackPosition.top ? 16 : 0,
    ),
    padding: EdgeInsets.zero,
    backgroundColor: Colors.transparent,
    elevation: 0,
    duration: duration,
    dismissDirection: dismissible
        ? DismissDirection.horizontal
        : DismissDirection.none,
    content: _SnackContent(
      message: message,
      config: config,
      actionLabel: actionLabel,
      onAction: onAction,
      showIcon: showIcon,
    ),
  );

  ScaffoldMessenger.of(context)
    ..clearSnackBars()
    ..showSnackBar(snackBar);
}

void showMyToast(
  BuildContext context, {
  required String message,
  MySnackType type = MySnackType.neutral,
  Duration duration = const Duration(seconds: 2),
  bool showIcon = true,
}) {
  final overlay = Overlay.maybeOf(context);
  if (overlay == null) {
    showMySnackBar(
      context,
      message: message,
      type: type,
      duration: duration,
      showIcon: showIcon,
    );
    return;
  }

  final theme = Theme.of(context);
  final config = _resolveSnackConfig(
    type,
    theme.colorScheme,
    AppSemanticColors.of(context),
  );
  late final OverlayEntry entry;

  entry = OverlayEntry(
    builder: (_) => _ToastOverlay(
      config: config,
      message: message,
      duration: duration,
      showIcon: showIcon,
      onDismissed: () => entry.remove(),
    ),
  );

  overlay.insert(entry);
}

class _SnackContent extends StatelessWidget {
  const _SnackContent({
    required this.message,
    required this.config,
    this.actionLabel,
    this.onAction,
    this.showIcon = true,
  });

  final String message;
  final _SnackConfig config;
  final String? actionLabel;
  final VoidCallback? onAction;
  final bool showIcon;

  @override
  Widget build(BuildContext context) {
    final textStyle = Theme.of(context).textTheme.bodyMedium?.copyWith(
      color: config.foreground,
      fontWeight: FontWeight.w600,
    );

    return DecoratedBox(
      decoration: BoxDecoration(
        color: config.background,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: config.background.withValues(alpha: 0.30),
            blurRadius: 16,
            offset: const Offset(0, 6),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsetsDirectional.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            if (showIcon) ...[
              Icon(config.icon, color: config.foreground, size: 20),
              const SizedBox(width: 10),
            ],
            Expanded(child: Text(message, style: textStyle)),
            if (actionLabel != null) ...[
              const SizedBox(width: 12),
              TextButton(
                style: TextButton.styleFrom(
                  foregroundColor: config.foreground,
                  padding: const EdgeInsetsDirectional.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  minimumSize: Size.zero,
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
                onPressed: () {
                  ScaffoldMessenger.of(context).clearSnackBars();
                  onAction?.call();
                },
                child: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _ToastOverlay extends StatefulWidget {
  const _ToastOverlay({
    required this.config,
    required this.message,
    required this.duration,
    required this.showIcon,
    required this.onDismissed,
  });

  final _SnackConfig config;
  final String message;
  final Duration duration;
  final bool showIcon;
  final VoidCallback onDismissed;

  @override
  State<_ToastOverlay> createState() => _ToastOverlayState();
}

class _ToastOverlayState extends State<_ToastOverlay>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _opacity;
  late final Animation<Offset> _offset;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 180),
      reverseDuration: const Duration(milliseconds: 140),
    );
    _opacity = CurvedAnimation(parent: _controller, curve: Curves.easeOut);
    _offset = Tween<Offset>(
      begin: const Offset(0, -0.12),
      end: Offset.zero,
    ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOut));

    _controller.forward();
    Future<void>.delayed(widget.duration, () async {
      if (!mounted) return;
      await _controller.reverse();
      widget.onDismissed();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return PositionedDirectional(
      top: MediaQuery.viewPaddingOf(context).top + 16,
      start: 16,
      end: 16,
      child: SafeArea(
        child: FadeTransition(
          opacity: _opacity,
          child: SlideTransition(
            position: _offset,
            child: Material(
              color: Colors.transparent,
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 420),
                  child: _SnackContent(
                    message: widget.message,
                    config: widget.config,
                    showIcon: widget.showIcon,
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
