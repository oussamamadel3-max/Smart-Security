import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';

class LoginState {
  const LoginState({
    this.isSubmitting = false,
    this.error,
    this.userName = '',
    this.password = '',
  });

  final bool isSubmitting;
  final ErrorState? error;
  final String userName;
  final String password;

  LoginState copyWith({
    bool? isSubmitting,
    ErrorState? error,
    bool clearError = false,
    String? userName,
    String? password,
  }) {
    return LoginState(
      isSubmitting: isSubmitting ?? this.isSubmitting,
      error: clearError ? null : (error ?? this.error),
      userName: userName ?? this.userName,
      password: password ?? this.password,
    );
  }
}

class LoginViewModel extends Notifier<LoginState> {
  @override
  LoginState build() => const LoginState();

  void setUserName(String value) {
    state = state.copyWith(userName: value, clearError: true);
  }

  void setPassword(String value) {
    state = state.copyWith(password: value, clearError: true);
  }

  Future<void> submit() async {
    final userName = state.userName.trim();
    final password = state.password;
    final userError = AppValidators.required(userName);
    final passError = AppValidators.required(password);
    if (userError != null || passError != null) {
      state = state.copyWith(
        error: ErrorState(
          message: userError ?? passError ?? 'هذا الحقل إلزامي',
          displayType: ErrorDisplayType.inline,
        ),
      );
      return;
    }

    state = state.copyWith(isSubmitting: true, clearError: true);
    final result = await ref.read(authSessionProvider.notifier).login(
          userName: userName,
          password: password,
        );

    result.fold(
      onFailure: (failure) {
        state = state.copyWith(
          isSubmitting: false,
          error: FailureHandler.handle(failure),
        );
      },
      onSuccess: (_) {
        state = state.copyWith(
          isSubmitting: false,
          password: '',
          clearError: true,
        );
      },
    );
  }
}

final loginViewModelProvider =
    NotifierProvider<LoginViewModel, LoginState>(LoginViewModel.new);
