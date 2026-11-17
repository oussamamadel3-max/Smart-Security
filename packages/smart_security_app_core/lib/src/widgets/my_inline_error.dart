import 'package:flutter/material.dart';
import '../errors/error_state.dart';

// عنصر لعرض خطأ داخل النموذج أو بجانب حقل.
// MyInlineError لرسائل خطأ عامة صغيرة.
// MyInlineFieldError لعرض رسالة خطأ حقل واحد.
// يدعم إعادة المحاولة داخل العنصر إذا كانت الحالة تدعم ذلك.

class MyInlineError extends StatelessWidget {
  const MyInlineError({
    super.key,
    required this.message,
    this.icon = Icons.error_outline_rounded,
    this.onRetry,
    this.retryLabel,
    this.margin,
  }) : assert(
         onRetry == null || retryLabel != null,
         'retryLabel is required whenever onRetry is provided — this '
         'widget never falls back to a hardcoded label (no hardcoded '
         'strings rule). Pass a localized string via context.tr.',
       );

  factory MyInlineError.fromState(
    ErrorState state, {
    Key? key,
    String? retryLabel,
    EdgeInsetsGeometry? margin,
  }) {
    return MyInlineError(
      key: key,
      message: state.message,
      onRetry: state.canRetry ? state.onAction : null,
      retryLabel: retryLabel ?? state.actionLabel,
      margin: margin,
    );
  }

  final String message;
  final IconData icon;
  final VoidCallback? onRetry;
  final String? retryLabel;
  final EdgeInsetsGeometry? margin;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final Color color = theme.colorScheme.error;
    final TextStyle textStyle =
        theme.textTheme.bodySmall?.copyWith(color: color) ?? TextStyle();

    return AnimatedSize(
      duration: const Duration(milliseconds: 200),
      curve: Curves.easeOut,
      alignment: AlignmentDirectional.topStart,
      child: Container(
        key: ValueKey(message),
        margin: margin ?? const EdgeInsets.only(top: 6),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: color.withValues(alpha: 0.24)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 16, color: color),
            const SizedBox(width: 8),
            Expanded(child: Text(message, style: textStyle)),
            if (onRetry != null) ...[
              const SizedBox(width: 8),
              GestureDetector(
                onTap: onRetry,
                child: Text(
                  retryLabel!,
                  style: textStyle.copyWith(
                    fontWeight: FontWeight.w600,
                    decoration: TextDecoration.underline,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class MyInlineFieldError extends StatelessWidget {
  const MyInlineFieldError({super.key, required this.child, this.errorText});
  factory MyInlineFieldError.forField({
    required Widget child,
    required String fieldName,
    ErrorState? errorState,
    Key? key,
  }) {
    final bool hasError = errorState?.hasFieldError(fieldName) ?? false;
    return MyInlineFieldError(
      key: key,
      errorText: hasError ? errorState!.fieldError(fieldName) : null,
      child: child,
    );
  }

  final Widget child;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final Color color = theme.colorScheme.error;
    final bool hasError = errorText != null && errorText!.isNotEmpty;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        child,
        AnimatedSize(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOut,
          alignment: AlignmentDirectional.topStart,
          child: !hasError
              ? const SizedBox.shrink()
              : Padding(
                  padding: const EdgeInsetsDirectional.only(start: 4, top: 4),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.error_outline_rounded, size: 13, color: color),
                      const SizedBox(width: 4),
                      Flexible(
                        child: Text(
                          errorText!,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: color,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
        ),
      ],
    );
  }
}
