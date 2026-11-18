import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../features/auth/domain/entities/auth_session_tokens.dart';
import '../../features/auth/domain/entities/auth_user_entity.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../../features/auth/domain/usecases/login_usecase.dart';
import '../../features/auth/domain/usecases/logout_usecase.dart';
import '../../features/auth/domain/usecases/restore_session_usecase.dart';
import '../network/auth_dependency_providers.dart';
import 'access_token_holder.dart';
import 'auth_session_state.dart';
import 'csrf_token_reader.dart';
import 'session_epoch.dart';

/// Single session owner for the web app (`authSessionProvider`).
class AuthSessionController extends Notifier<AuthSessionState> {
  bool _logoutInProgress = false;

  AccessTokenHolder get _access => ref.read(accessTokenHolderProvider);
  AuthRepository get _repository => ref.read(authRepositoryProvider);
  CsrfTokenReader get _csrf => ref.read(csrfTokenReaderProvider);

  bool get isLogoutInProgress => _logoutInProgress;

  @override
  AuthSessionState build() => AuthSessionState.initial;

  Future<void> restoreSession() async {
    state = state.copyWith(status: AuthStatus.unknown, clearUser: true);

    // Cookie-channel restore requires a readable CSRF cookie + HttpOnly refresh.
    final csrf = _csrf.read();
    if (csrf == null || csrf.isEmpty) {
      _access.clear();
      state = state.copyWith(
        status: AuthStatus.unauthenticated,
        clearUser: true,
      );
      return;
    }

    final generation = state.generation;
    final result = await RestoreSessionUseCase(_repository)();
    if (generation != state.generation || _logoutInProgress) {
      return;
    }

    await result.fold(
      onFailure: (_) async {
        await _hardClear(sessionExpired: false);
      },
      onSuccess: (tokens) async {
        if (!_acceptWebUser(tokens.user)) {
          await _hardClear(sessionExpired: false);
          return;
        }
        await _persistSession(tokens, generation: generation);
      },
    );
  }

  Future<Result<AuthUserEntity>> login({
    required String userName,
    required String password,
  }) async {
    final result = await LoginUseCase(_repository)(
      userName: userName,
      password: password,
    );

    switch (result) {
      case Failure_(:final failure):
        return Failure_(failure);
      case Success(:final data):
        if (!_acceptWebUser(data.user)) {
          _access.clear();
          return const Failure_(
            Failure.forbidden(
              message: 'هذا التطبيق مخصص لحسابات المالك أو الأدمن.',
            ),
          );
        }
        await _persistSession(data, generation: state.generation);
        return Success(data.user);
    }
  }

  /// Single-flight refresh target used by AuthInterceptor.
  Future<bool> refreshSession() async {
    if (_logoutInProgress) return false;

    final generation = state.generation;
    final csrf = _csrf.read();
    if (csrf == null || csrf.isEmpty) return false;

    final result = await RestoreSessionUseCase(_repository)();
    if (_logoutInProgress || generation != state.generation) {
      return false;
    }

    switch (result) {
      case Failure_():
        if (!_logoutInProgress && generation == state.generation) {
          await _hardClear(sessionExpired: true);
        }
        return false;
      case Success(:final data):
        if (_logoutInProgress || generation != state.generation) {
          return false;
        }
        if (!_acceptWebUser(data.user)) {
          await _hardClear(sessionExpired: true);
          return false;
        }
        await _persistSession(data, generation: generation);
        return true;
    }
  }

  void markSessionExpired() {
    if (_logoutInProgress) return;
    // A finished logout stays unauthenticated. A second expiry must not bump
    // generation again or it can discard a newer session.
    if (state.status == AuthStatus.unauthenticated ||
        state.status == AuthStatus.sessionExpired) {
      return;
    }
    _access.clear();
    ref.read(sessionEpochProvider).invalidate();
    state = state.copyWith(
      status: AuthStatus.sessionExpired,
      clearUser: true,
      generation: state.generation + 1,
    );
  }

  Future<void> logout() async {
    if (_logoutInProgress) return;
    _logoutInProgress = true;
    final generation = state.generation;

    try {
      await LogoutUseCase(_repository)();
    } catch (_) {
      // Local clear still required.
    } finally {
      await _hardClear(sessionExpired: false, bumpGenerationFrom: generation);
      _logoutInProgress = false;
    }
  }

  Future<void> logoutAll() async {
    if (_logoutInProgress) return;
    _logoutInProgress = true;
    final generation = state.generation;

    try {
      await LogoutUseCase(_repository).logoutAll();
    } catch (_) {
      // Local clear still required.
    } finally {
      await _hardClear(sessionExpired: false, bumpGenerationFrom: generation);
      _logoutInProgress = false;
    }
  }

  Future<void> _persistSession(
    AuthSessionTokens tokens, {
    required int generation,
  }) async {
    if (_logoutInProgress || generation != state.generation) {
      return;
    }
    final previousUserId = state.user?.id;
    _access.setToken(tokens.accessToken);
    if (_logoutInProgress || generation != state.generation) {
      _access.clear();
      return;
    }
    final switchedUser =
        previousUserId != null && previousUserId != tokens.user.id;
    if (switchedUser) {
      ref.read(sessionEpochProvider).invalidate();
    }
    state = state.copyWith(
      status: AuthStatus.authenticated,
      user: tokens.user,
      generation: switchedUser ? generation + 1 : generation,
    );
  }

  Future<void> _hardClear({
    required bool sessionExpired,
    int? bumpGenerationFrom,
  }) async {
    _access.clear();
    ref.read(sessionEpochProvider).invalidate();
    final nextGen = (bumpGenerationFrom ?? state.generation) + 1;
    state = AuthSessionState(
      status: sessionExpired
          ? AuthStatus.sessionExpired
          : AuthStatus.unauthenticated,
      generation: nextGen,
    );
  }

  bool _acceptWebUser(AuthUserEntity user) {
    if (user.isOwner || user.isAdmin) return true;
    if (user.roles.isEmpty) return false;
    return user.roles.any((r) {
      final code = r.toUpperCase();
      return code == AppRoleCodes.owner || code == AppRoleCodes.admin;
    });
  }
}

final authSessionProvider =
    NotifierProvider<AuthSessionController, AuthSessionState>(
      AuthSessionController.new,
    );
