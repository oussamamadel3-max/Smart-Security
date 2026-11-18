import 'dart:async';
import 'dart:convert';

import 'package:admin_web/core/network/auth_dependency_providers.dart';
import 'package:admin_web/core/session/access_token_holder.dart';
import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/csrf_token_reader.dart';
import 'package:admin_web/features/auth/domain/entities/auth_session_tokens.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/auth/domain/repositories/auth_repository.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/entities/owner_device_entity.dart';
import 'package:admin_web/features/devices/domain/repositories/device_commands_repository.dart';
import 'package:admin_web/features/devices/domain/repositories/devices_repository.dart';
import 'package:admin_web/features/devices/domain/usecases/list_my_devices_usecase.dart';
import 'package:admin_web/features/devices/presentation/providers/devices_providers.dart';
import 'package:admin_web/features/devices/presentation/viewmodels/device_detail_view_model.dart';
import 'package:admin_web/features/devices/presentation/viewmodels/devices_list_view_model.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

String _jwt(List<String> roles) {
  String part(String json) =>
      base64Url.encode(utf8.encode(json)).replaceAll('=', '');
  return '${part('{"alg":"none"}')}.${part(jsonEncode({'role': roles}))}.sig';
}

class _AuthRepo implements AuthRepository {
  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async {
    return Success(
      AuthSessionTokens(
        accessToken: _jwt(const ['OWNER']),
        expiresIn: 60,
        refreshChannel: 'COOKIE',
        user: const AuthUserEntity(
          id: 'owner-1',
          userName: 'owner1',
          roles: ['OWNER'],
        ),
      ),
    );
  }

  @override
  Future<Result<bool>> logout() async => const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<AuthSessionTokens>> refresh() async =>
      const Failure_(Failure.unauthorized());
}

class _IdleDevices implements DevicesRepository {
  int clears = 0;

  @override
  void clearCache() => clears++;

  @override
  Future<Result<OwnerDeviceEntity>> getMine(String deviceId) async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<List<OwnerDeviceEntity>>> listMine() async => const Success([]);
}

class _LateList extends ListMyDevicesUseCase {
  _LateList(this.gate, this.rows) : super(_IdleDevices());

  final Completer<void> gate;
  final List<OwnerDeviceEntity> rows;

  @override
  Future<Result<List<OwnerDeviceEntity>>> call() async {
    await gate.future;
    return Success(rows);
  }
}

class _SlowDevices implements DevicesRepository {
  final Completer<Result<OwnerDeviceEntity>> gate = Completer();

  @override
  void clearCache() {}

  @override
  Future<Result<OwnerDeviceEntity>> getMine(String deviceId) => gate.future;

  @override
  Future<Result<List<OwnerDeviceEntity>>> listMine() async => const Success([]);
}

class _Commands implements DeviceCommandsRepository {
  @override
  void clearCache() {}

  @override
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) async => const Failure_(Failure.unexpected());

  @override
  void dropDevice(String deviceId) {}

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async => const Failure_(Failure.unexpected());

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async => const Success([]);
}

const _previousDevice = OwnerDeviceEntity(
  deviceId: 'device-a',
  displayName: 'جهاز المالك السابق',
  ownershipStatus: OwnershipStatus.active,
  ownershipVersion: 1,
  confirmedOwnershipVersion: 1,
  awaitingOwnershipAck: false,
  canControl: true,
  linkStatusMessage: 'الملكية فعالة',
);

ProviderContainer _container({
  required ListMyDevicesUseCase listUseCase,
  required DevicesRepository devices,
}) {
  return ProviderContainer(
    overrides: [
      accessTokenHolderProvider.overrideWithValue(AccessTokenHolder()),
      csrfTokenReaderProvider.overrideWithValue(MemoryCsrfTokenReader('csrf')),
      authRepositoryProvider.overrideWithValue(_AuthRepo()),
      listMyDevicesUseCaseProvider.overrideWithValue(listUseCase),
      devicesRepositoryProvider.overrideWithValue(devices),
      deviceCommandsRepositoryProvider.overrideWithValue(_Commands()),
    ],
  );
}

void main() {
  test('late device list success after logout does not restore rows', () async {
    final gate = Completer<void>();
    final devices = _IdleDevices();
    final container = _container(
      listUseCase: _LateList(gate, const [_previousDevice]),
      devices: devices,
    );
    addTearDown(container.dispose);

    final session = container.read(authSessionProvider.notifier);
    await session.login(userName: 'owner1', password: 'x');
    container.read(devicesListViewModelProvider);
    await Future<void>.delayed(Duration.zero);

    await session.logout();
    gate.complete();
    await Future<void>.delayed(Duration.zero);

    final state = container.read(devicesListViewModelProvider);
    expect(state.devices, isEmpty);
    expect(container.read(authSessionProvider).isAuthenticated, isFalse);
    expect(devices.clears, greaterThan(0));
  });

  test(
    'late device detail success after logout does not restore the device',
    () async {
      final devices = _SlowDevices();
      final container = _container(
        listUseCase: _LateList(Completer<void>(), const []),
        devices: devices,
      );
      addTearDown(container.dispose);

      final session = container.read(authSessionProvider.notifier);
      await session.login(userName: 'owner1', password: 'x');
      container.read(deviceDetailViewModelProvider('device-a'));
      await Future<void>.delayed(Duration.zero);

      await session.logout();
      devices.gate.complete(const Success(_previousDevice));
      await Future<void>.delayed(Duration.zero);

      expect(
        container.read(deviceDetailViewModelProvider('device-a')).device,
        isNull,
      );
      expect(container.read(authSessionProvider).isAuthenticated, isFalse);
    },
  );
}
