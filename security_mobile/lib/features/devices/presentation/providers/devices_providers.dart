import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/mobile_network_providers.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../data/datasources/device_alarm_events_remote_datasource.dart';
import '../../data/datasources/device_commands_remote_datasource.dart';
import '../../data/datasources/devices_remote_datasource.dart';
import '../../data/repositories/device_alarm_events_repository_impl.dart';
import '../../data/repositories/device_commands_repository_impl.dart';
import '../../data/repositories/devices_repository_impl.dart';
import '../../domain/repositories/device_alarm_events_repository.dart';
import '../../domain/repositories/device_commands_repository.dart';
import '../../domain/repositories/devices_repository.dart';
import '../../domain/usecases/get_my_device_usecase.dart';
import '../../domain/usecases/list_my_devices_usecase.dart';

final devicesRepositoryProvider = Provider<DevicesRepository>((ref) {
  final repo = DevicesRepositoryImpl(
    DevicesRemoteDataSource(ref.watch(mobileDioProvider)),
  );

  ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
    if (previous?.isAuthenticated == true && !next.isAuthenticated) {
      repo.clearCache();
    }
    if (previous?.user?.id != null &&
        next.user?.id != null &&
        previous!.user!.id != next.user!.id) {
      repo.clearCache();
    }
  });

  return repo;
});

final listMyDevicesUseCaseProvider = Provider<ListMyDevicesUseCase>((ref) {
  return ListMyDevicesUseCase(ref.watch(devicesRepositoryProvider));
});

final getMyDeviceUseCaseProvider = Provider<GetMyDeviceUseCase>((ref) {
  return GetMyDeviceUseCase(ref.watch(devicesRepositoryProvider));
});

final deviceCommandsRepositoryProvider =
    Provider<DeviceCommandsRepository>((ref) {
  return DeviceCommandsRepositoryImpl(
    DeviceCommandsRemoteDataSource(ref.watch(mobileDioProvider)),
  );
});

final deviceAlarmEventsRepositoryProvider =
    Provider<DeviceAlarmEventsRepository>((ref) {
  final repo = DeviceAlarmEventsRepositoryImpl(
    DeviceAlarmEventsRemoteDataSource(ref.watch(mobileDioProvider)),
  );

  ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
    if (previous?.isAuthenticated == true && !next.isAuthenticated) {
      repo.clearCache();
    }
    if (previous?.user?.id != null &&
        next.user?.id != null &&
        previous!.user!.id != next.user!.id) {
      repo.clearCache();
    }
  });

  return repo;
});
