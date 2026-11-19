import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/push/push_token_id_store.dart';
import 'package:security_mobile/features/notifications/data/datasources/push_tokens_remote_datasource.dart';
import 'package:security_mobile/features/notifications/data/models/push_token_dto.dart';
import 'package:security_mobile/features/notifications/data/repositories/push_tokens_repository_impl.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Overrides every network call so no real HTTP request is ever made; the
/// `Dio()` passed to `super` is never touched.
class _FakeRemote extends PushTokensRemoteDataSource {
  _FakeRemote() : super(Dio());

  int registerCalls = 0;
  int revokeByIdCalls = 0;
  int revokeAllCalls = 0;

  /// Results returned in order on successive `register()` calls; the last
  /// one repeats once exhausted.
  List<Result<PushTokenResponseDto>> registerResults = [
    const Success(
      PushTokenResponseDto(
        pushTokenId: 'pt-1',
        platform: 'ANDROID',
        registeredAt: '2026-11-19T00:00:00Z',
        isActive: true,
      ),
    ),
  ];

  List<Result<bool>> revokeByIdResults = [const Success(true)];
  List<Result<bool>> revokeAllResults = [const Success(true)];

  @override
  Future<Result<PushTokenResponseDto>> register(
    RegisterPushTokenRequestDto request,
  ) async {
    final index = registerCalls;
    registerCalls++;
    return registerResults[index.clamp(0, registerResults.length - 1)];
  }

  @override
  Future<Result<bool>> revokeById(String pushTokenId) async {
    final index = revokeByIdCalls;
    revokeByIdCalls++;
    return revokeByIdResults[index.clamp(0, revokeByIdResults.length - 1)];
  }

  @override
  Future<Result<bool>> revokeAll() async {
    final index = revokeAllCalls;
    revokeAllCalls++;
    return revokeAllResults[index.clamp(0, revokeAllResults.length - 1)];
  }
}

/// In-memory stand-in so these tests never touch the real secure-storage
/// platform channel (unavailable under plain `flutter_test`).
class _InMemoryIdStore extends PushTokenIdStore {
  _InMemoryIdStore() : super(const FlutterSecureStorage());

  String? _value;

  @override
  Future<String?> read() async => _value;

  @override
  Future<void> write(String pushTokenId) async => _value = pushTokenId;

  @override
  Future<void> clear() async => _value = null;
}

const _transientNetworkFailure = Failure.network(
  message: 'no connection',
  retryable: true,
);

void main() {
  group('PushTokensRepositoryImpl.register', () {
    test('retries a transient network failure then succeeds', () async {
      final remote = _FakeRemote()
        ..registerResults = [
          const Failure_<PushTokenResponseDto>(_transientNetworkFailure),
          const Success(
            PushTokenResponseDto(
              pushTokenId: 'pt-after-retry',
              platform: 'ANDROID',
              registeredAt: '2026-11-19T00:00:00Z',
              isActive: true,
            ),
          ),
        ];
      final idStore = _InMemoryIdStore();
      final repo = PushTokensRepositoryImpl(remote, idStore);

      final result = await repo.register(
        token: 'a-device-token-0000000000',
        platform: 'ANDROID',
      );

      expect(result, isA<Success<String>>());
      expect((result as Success<String>).data, 'pt-after-retry');
      expect(remote.registerCalls, 2);
      expect(await idStore.read(), 'pt-after-retry');
    });

    test('does not retry a non-retryable (validation) failure', () async {
      final remote = _FakeRemote()
        ..registerResults = [
          const Failure_<PushTokenResponseDto>(
            Failure.validation(message: 'bad token', errors: {}),
          ),
        ];
      final idStore = _InMemoryIdStore();
      final repo = PushTokensRepositoryImpl(remote, idStore);

      final result = await repo.register(
        token: 'a-device-token-0000000000',
        platform: 'ANDROID',
      );

      expect(result, isA<Failure_<String>>());
      expect(remote.registerCalls, 1);
      expect(await idStore.read(), isNull);
    });
  });

  group('PushTokensRepositoryImpl.revokeStoredOrAll', () {
    test(
      'network failure (even after retries) never claims revocation and '
      'preserves the stored id for a later retry',
      () async {
        final remote = _FakeRemote()
          ..revokeByIdResults = List.filled(
            5,
            const Failure_<bool>(_transientNetworkFailure),
          );
        final idStore = _InMemoryIdStore();
        await idStore.write('pt-stored');
        final repo = PushTokensRepositoryImpl(remote, idStore);

        final result = await repo.revokeStoredOrAll();

        expect(result.isFailure, isTrue);
        expect(remote.revokeByIdCalls, greaterThan(1));
        expect(await idStore.read(), 'pt-stored');
      },
    );

    test('success clears the stored id', () async {
      final remote = _FakeRemote();
      final idStore = _InMemoryIdStore();
      await idStore.write('pt-stored');
      final repo = PushTokensRepositoryImpl(remote, idStore);

      final result = await repo.revokeStoredOrAll();

      expect(result.isSuccess, isTrue);
      expect(remote.revokeByIdCalls, 1);
      expect(await idStore.read(), isNull);
    });

    test('with no stored id, revokes all and succeeds', () async {
      final remote = _FakeRemote();
      final idStore = _InMemoryIdStore();
      final repo = PushTokensRepositoryImpl(remote, idStore);

      final result = await repo.revokeStoredOrAll();

      expect(result.isSuccess, isTrue);
      expect(remote.revokeAllCalls, 1);
      expect(remote.revokeByIdCalls, 0);
    });
  });
}
