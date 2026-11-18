import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_indicators_entity.dart';
import 'package:admin_web/features/devices/domain/entities/owner_device_entity.dart';
import 'package:admin_web/features/devices/domain/repositories/device_commands_repository.dart';
import 'package:admin_web/features/devices/domain/repositories/devices_repository.dart';
import 'package:admin_web/features/devices/presentation/providers/devices_providers.dart';
import 'package:admin_web/features/devices/presentation/viewmodels/device_detail_view_model.dart';
import 'package:fake_async/fake_async.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _OwnerSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(id: 'owner-1', roles: ['OWNER']),
    );
  }
}

class _DevicesRepo implements DevicesRepository {
  _DevicesRepo(this.device);

  OwnerDeviceEntity device;

  @override
  Future<Result<List<OwnerDeviceEntity>>> listMine() async {
    return Result.success([device]);
  }

  @override
  Future<Result<OwnerDeviceEntity>> getMine(String deviceId) async {
    return Result.success(device);
  }

  @override
  void clearCache() {}
}

class _CommandsRepo implements DeviceCommandsRepository {
  int creates = 0;
  int gets = 0;
  CommandType? lastType;
  String? lastAlarmUid;
  DeviceCommandEntity response = _command(CommandStatus.pending);

  @override
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) async {
    creates += 1;
    lastType = type;
    lastAlarmUid = targetAlarmEventUid;
    return Result.success(response);
  }

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async {
    gets += 1;
    return Result.success(response);
  }

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async {
    return const Result.success([]);
  }

  @override
  void clearCache() {}

  @override
  void dropDevice(String deviceId) {}
}

DeviceCommandEntity _command(CommandStatus status) {
  return DeviceCommandEntity(
    commandId: 'c1',
    deviceId: 'd1',
    type: CommandType.arm,
    status: status,
    commandSequence: 1,
    ownershipVersion: 1,
    observedStateSequence: 1,
    createdAt: DateTime.now(),
    expiresAt: DateTime.now().add(const Duration(seconds: 30)),
    actualStateChanged: false,
    idempotentReplay: false,
  );
}

OwnerDeviceEntity _device({
  ArmStatus arm = ArmStatus.disarmed,
  SirenStatus siren = SirenStatus.off,
  String? alarmId,
  bool canControl = true,
}) {
  return OwnerDeviceEntity(
    deviceId: 'd1',
    displayName: 'منزل',
    ownershipStatus: OwnershipStatus.active,
    ownershipVersion: 1,
    confirmedOwnershipVersion: 1,
    awaitingOwnershipAck: false,
    canControl: canControl,
    linkStatusMessage: '',
    indicators: DeviceIndicatorsEntity(
      connection: ConnectionStatus.online,
      arm: arm,
      siren: siren,
      health: DiagnosticStatus.healthy,
      currentAlarmEventUid: alarmId,
    ),
  );
}

Future<DeviceDetailViewModel> _open({
  required ProviderContainer container,
  required String deviceId,
}) async {
  final notifier = container.read(
    deviceDetailViewModelProvider(deviceId).notifier,
  );
  // Let the notifier's startup load finish before the explicit reload,
  // so a late success cannot clear the error set by submitCommand.
  await Future<void>.delayed(const Duration(milliseconds: 30));
  await notifier.load(deviceId);
  return notifier;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('MUTE without current alarm id does not POST', () async {
    final devices = _DevicesRepo(_device());
    final commands = _CommandsRepo();
    final container = _container(devices, commands);
    addTearDown(container.dispose);

    final vm = await _open(container: container, deviceId: 'd1');
    await vm.submitCommand(CommandType.muteAlarm);

    final state = container.read(deviceDetailViewModelProvider('d1'));
    expect(commands.creates, 0);
    expect(state.error?.message, contains('كتم'));
    expect(state.device?.indicators?.arm, ArmStatus.disarmed);
  });

  test('TEST is blocked while siren is ALARM', () async {
    final devices = _DevicesRepo(
      _device(siren: SirenStatus.alarm, alarmId: 'alarm-1'),
    );
    final commands = _CommandsRepo();
    final container = _container(devices, commands);
    addTearDown(container.dispose);

    final vm = await _open(container: container, deviceId: 'd1');
    await vm.submitCommand(CommandType.testSiren);

    expect(commands.creates, 0);
    expect(
      container.read(deviceDetailViewModelProvider('d1')).error?.message,
      contains('اختبار'),
    );
  });

  test('STOP_TEST is allowed only during TEST', () async {
    final devices = _DevicesRepo(_device(siren: SirenStatus.off));
    final commands = _CommandsRepo();
    final container = _container(devices, commands);
    addTearDown(container.dispose);

    final vm = await _open(container: container, deviceId: 'd1');
    await vm.submitCommand(CommandType.stopTest);

    expect(commands.creates, 0);
    expect(
      container.read(deviceDetailViewModelProvider('d1')).error?.message,
      contains('إيقاف الاختبار'),
    );
  });

  test('HTTP PENDING does not flip confirmed arm state', () async {
    final devices = _DevicesRepo(
      _device(arm: ArmStatus.disarmed, alarmId: 'alarm-9'),
    );
    final commands = _CommandsRepo();
    final container = _container(devices, commands);
    addTearDown(container.dispose);

    final vm = await _open(container: container, deviceId: 'd1');
    await vm.submitCommand(CommandType.muteAlarm);

    final state = container.read(deviceDetailViewModelProvider('d1'));
    expect(commands.creates, 1);
    expect(commands.lastType, CommandType.muteAlarm);
    expect(commands.lastAlarmUid, 'alarm-9');
    expect(state.pendingCommand?.status, CommandStatus.pending);
    expect(state.pendingCommand?.actualStateChanged, isFalse);
    expect(state.device?.indicators?.arm, ArmStatus.disarmed);
    expect(state.device?.indicators?.siren, SirenStatus.off);
  });

  test('unresolved TIMED_OUT blocks a second command', () async {
    final devices = _DevicesRepo(_device(arm: ArmStatus.armed));
    final commands = _CommandsRepo()
      ..response = _command(CommandStatus.timedOut);
    final container = _container(devices, commands);
    addTearDown(container.dispose);

    final vm = await _open(container: container, deviceId: 'd1');
    await vm.submitCommand(CommandType.disarm);
    await vm.submitCommand(CommandType.arm);

    final state = container.read(deviceDetailViewModelProvider('d1'));
    expect(commands.creates, 1);
    expect(state.hasUnresolvedTimeout, isTrue);
    expect(state.device?.indicators?.arm, ArmStatus.armed);
    expect(state.error?.message, contains('المصالحة'));
  });

  test('pausePolling stops further command status GETs', () {
    fakeAsync((async) {
      final devices = _DevicesRepo(_device(alarmId: 'alarm-9'));
      final commands = _CommandsRepo();
      final container = _container(devices, commands);
      addTearDown(container.dispose);

      final vm = container.read(deviceDetailViewModelProvider('d1').notifier);
      async.flushMicrotasks();
      async.flushMicrotasks();

      vm.submitCommand(CommandType.muteAlarm);
      async.flushMicrotasks();
      expect(commands.creates, 1);
      expect(commands.gets, 0);

      async.elapse(const Duration(seconds: 2));
      expect(commands.gets, 1);

      vm.pausePolling();
      async.elapse(const Duration(seconds: 6));
      expect(commands.gets, 1);

      vm.resumePollingIfNeeded();
      async.elapse(const Duration(seconds: 2));
      expect(commands.gets, 2);
    });
  });
}

ProviderContainer _container(_DevicesRepo devices, _CommandsRepo commands) {
  return ProviderContainer(
    overrides: [
      authSessionProvider.overrideWith(_OwnerSession.new),
      devicesRepositoryProvider.overrideWith((ref) => devices),
      deviceCommandsRepositoryProvider.overrideWith((ref) => commands),
    ],
  );
}
