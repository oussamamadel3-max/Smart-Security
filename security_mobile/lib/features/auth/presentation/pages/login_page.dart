import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../viewmodels/login_view_model.dart';

/// Owner login screen mapped from `design/index.html` login view.
///
/// Design prototype used an email field; Backend Auth expects `userName`.
class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  late final TextEditingController _userController;
  late final TextEditingController _passwordController;
  bool _obscure = true;

  @override
  void initState() {
    super.initState();
    _userController = TextEditingController();
    _passwordController = TextEditingController();
  }

  @override
  void dispose() {
    _userController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final state = ref.watch(loginViewModelProvider);
    final vm = ref.read(loginViewModelProvider.notifier);
    final env = AppEnvironment.current;
    final theme = Theme.of(context);

    ref.listen<LoginState>(loginViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType == ErrorDisplayType.snackbar ||
          error.displayType == ErrorDisplayType.dialog) {
        showErrorSnackBar(context, error);
      }
    });

    return Scaffold(
      backgroundColor: DesignTokens.background,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(DesignTokens.spaceXl),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    tr.appName,
                    style: theme.textTheme.headlineLarge,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: DesignTokens.spaceSm),
                  Text(
                    tr.welcomeBack,
                    style: theme.textTheme.titleLarge,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: DesignTokens.spaceSm),
                  Text(
                    tr.loginSubtitle,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: DesignTokens.muted,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: DesignTokens.spaceMd),
                  Text(
                    'بيئة التشغيل: ${env.label}',
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: DesignTokens.muted,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: DesignTokens.spaceXl),
                  DecoratedBox(
                    decoration: BoxDecoration(
                      color: DesignTokens.card,
                      borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
                      border: Border.all(color: DesignTokens.border),
                      boxShadow: DesignTokens.cardShadow,
                    ),
                    child: Padding(
                      padding: const EdgeInsets.all(DesignTokens.spaceXl),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text(tr.username, style: theme.textTheme.labelLarge),
                          const SizedBox(height: DesignTokens.spaceSm),
                          TextField(
                            controller: _userController,
                            enabled: !state.isSubmitting,
                            textInputAction: TextInputAction.next,
                            autocorrect: false,
                            decoration: InputDecoration(
                              hintText: tr.username,
                            ),
                            onChanged: vm.setUserName,
                          ),
                          const SizedBox(height: DesignTokens.spaceLg),
                          Text(tr.password, style: theme.textTheme.labelLarge),
                          const SizedBox(height: DesignTokens.spaceSm),
                          TextField(
                            controller: _passwordController,
                            enabled: !state.isSubmitting,
                            obscureText: _obscure,
                            textInputAction: TextInputAction.done,
                            onSubmitted: (_) => vm.submit(),
                            decoration: InputDecoration(
                              hintText: tr.password,
                              suffixIcon: IconButton(
                                onPressed: () =>
                                    setState(() => _obscure = !_obscure),
                                icon: Icon(
                                  _obscure
                                      ? Icons.visibility_outlined
                                      : Icons.visibility_off_outlined,
                                ),
                              ),
                            ),
                            onChanged: vm.setPassword,
                          ),
                          if (state.error != null &&
                              state.error!.displayType ==
                                  ErrorDisplayType.inline) ...[
                            const SizedBox(height: DesignTokens.spaceMd),
                            MyInlineError(message: state.error!.message),
                          ],
                          const SizedBox(height: DesignTokens.spaceXl),
                          ElevatedButton(
                            onPressed: state.isSubmitting ? null : vm.submit,
                            child: state.isSubmitting
                                ? Row(
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      const SizedBox(
                                        width: 18,
                                        height: 18,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      ),
                                      const SizedBox(
                                        width: DesignTokens.spaceMd,
                                      ),
                                      Text(tr.signingIn),
                                    ],
                                  )
                                : Text(tr.login),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
