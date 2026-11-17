import 'package:flutter/material.dart';
import '../errors/error_state.dart';
import 'my_dialog.dart';
import 'my_empty_state.dart';
import 'my_snackbar.dart';

/// يطبّق [ErrorState.displayType] على الواجهة:
/// Snackbar / Dialog تُعرض كأثر جانبي، أما Inline و FullScreen فتُبنى في الشجرة.
void dispatchErrorDisplay(BuildContext context, ErrorState state) {
  switch (state.displayType) {
    case ErrorDisplayType.snackbar:
      showErrorSnackBar(context, state);
    case ErrorDisplayType.dialog:
      showErrorDialog(context, state);
    case ErrorDisplayType.inline:
    case ErrorDisplayType.fullScreen:
      break;
  }
}

/// يستبدل المحتوى بحالة شاشة كاملة عندما يكون العرض FullScreen.
class ErrorStateHost extends StatelessWidget {
  const ErrorStateHost({super.key, required this.error, required this.child});

  final ErrorState? error;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final current = error;
    if (current != null && current.displayType == ErrorDisplayType.fullScreen) {
      return MyEmptyState.fromErrorState(state: current, size: MyEmptySize.lg);
    }
    return child;
  }
}
