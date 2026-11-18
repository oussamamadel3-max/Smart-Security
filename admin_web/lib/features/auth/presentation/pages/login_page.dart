import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../../../core/widgets/owner_visual.dart';
import '../viewmodels/login_view_model.dart';

/// Owner/Admin login screen mapped from `design/index.html` login view.
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
    final sessionExpired = ref.watch(
      authSessionProvider.select(
        (session) => session.status == AuthStatus.sessionExpired,
      ),
    );
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
              constraints: const BoxConstraints(
                maxWidth: OwnerLayout.loginMaxWidth,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      const OwnerBrandMark(),
                      const SizedBox(width: DesignTokens.spaceMd),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              tr.appName,
                              style: theme.textTheme.headlineSmall?.copyWith(
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            Text(
                              tr.appTitle,
                              style: theme.textTheme.labelSmall?.copyWith(
                                color: DesignTokens.muted,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: DesignTokens.spaceXl),
                  OwnerPageHeader(
                    title: tr.welcomeBack,
                    subtitle: tr.loginSubtitle,
                  ),
                  DecoratedBox(
                    decoration: BoxDecoration(
                      color: DesignTokens.card,
                      borderRadius: BorderRadius.circular(
                        DesignTokens.radiusXl,
                      ),
                      border: Border.all(color: DesignTokens.border),
                      boxShadow: DesignTokens.cardShadow,
                    ),
                    child: Padding(
                      padding: const EdgeInsets.all(DesignTokens.spaceXl),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          if (sessionExpired) ...[
                            _SessionExpiredNotice(
                              title: tr.sessionExpiredTitle,
                              message: tr.sessionExpiredMessage,
                            ),
                            const SizedBox(height: DesignTokens.spaceLg),
                          ],
                          Text(
                            tr.username,
                            style: theme.textTheme.labelMedium?.copyWith(
                              fontWeight: FontWeight.w700,
                              color: DesignTokens.muted,
                            ),
                          ),
                          const SizedBox(height: DesignTokens.spaceSm),
                          TextField(
                            controller: _userController,
                            enabled: !state.isSubmitting,
                            textInputAction: TextInputAction.next,
                            autocorrect: false,
                            decoration: InputDecoration(
                              hintText: tr.username,
                              errorText: state.field == 'userName'
                                  ? state.error?.message
                                  : null,
                            ),
                            onChanged: vm.setUserName,
                          ),
                          const SizedBox(height: DesignTokens.spaceLg),
                          Text(
                            tr.password,
                            style: theme.textTheme.labelMedium?.copyWith(
                              fontWeight: FontWeight.w700,
                              color: DesignTokens.muted,
                            ),
                          ),
                          const SizedBox(height: DesignTokens.spaceSm),
                          TextField(
                            controller: _passwordController,
                            enabled: !state.isSubmitting,
                            obscureText: _obscure,
                            textInputAction: TextInputAction.done,
                            onSubmitted: (_) => vm.submit(),
                            decoration: InputDecoration(
                              hintText: tr.password,
                              errorText: state.field == 'password'
                                  ? state.error?.message
                                  : null,
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
                              state.field == null &&
                              state.error!.displayType ==
                                  ErrorDisplayType.inline) ...[
                            const SizedBox(height: DesignTokens.spaceMd),
                            MyInlineError(message: state.error!.message),
                          ],
                          const SizedBox(height: DesignTokens.spaceXl),
                          SizedBox(
                            width: double.infinity,
                            child: ElevatedButton(
                              onPressed: state.isSubmitting ? null : vm.submit,
                              child: state.isSubmitting
                                  ? Row(
                                      mainAxisAlignment:
                                          MainAxisAlignment.center,
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
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: DesignTokens.spaceLg),
                  Text(
                    'ويب · ${env.label} · Access في الذاكرة · Refresh Cookie',
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: DesignTokens.muted,
                    ),
                    textAlign: TextAlign.center,
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

class _SessionExpiredNotice extends StatelessWidget {
  const _SessionExpiredNotice({required this.title, required this.message});

  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Semantics(
      liveRegion: true,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(
          horizontal: 15,
          vertical: DesignTokens.spaceMd,
        ),
        decoration: BoxDecoration(
          color: DesignTokens.noticeBg,
          borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
          border: Border.all(color: DesignTokens.noticeBorder),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: theme.textTheme.labelLarge?.copyWith(
                color: DesignTokens.noticeText,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: DesignTokens.spaceXs),
            Text(
              message,
              style: theme.textTheme.bodySmall?.copyWith(
                color: DesignTokens.noticeText,
                height: 1.6,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
