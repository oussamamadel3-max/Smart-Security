import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/web_network_providers.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../../../core/session/session_epoch.dart';
import '../../data/datasources/device_alarm_events_remote_datasource.dart';
import '../../data/datasources/device_commands_remote_datasource.dart';
import '../../data/datasources/devices_remote_datasource.dart';
import '../../data/repositories/device_alarm_events_repository_impl.dart';
import '../../data/repositories/device_commands_repository_impl.dart';
import '../../data/repositories/devices_repository_impl.dart';
import '../../domain/history_session_policy.dart';
import '../../domain/repositories/device_alarm_events_repository.dart';
import '../../domain/repositories/device_commands_repository.dart';
import '../../domain/repositories/devices_repository.dart';
import '../../domain/usecases/create_device_command_usecase.dart';
import '../../domain/usecases/get_device_alarm_event_usecase.dart';
import '../../domain/usecases/get_device_command_usecase.dart';
import '../../domain/usecases/get_my_device_usecase.dart';
import '../../domain/usecases/list_device_alarm_events_usecase.dart';
import '../../domain/usecases/list_device_commands_usecase.dart';
import '../../domain/usecases/list_my_devices_usecase.dart';

final devicesRepositoryProvider = Provider<DevicesRepository>((ref) {
  final repo = DevicesRepositoryImpl(
    DevicesRemoteDataSource(ref.watch(webDioProvider)),
    epoch: ref.watch(sessionEpochProvider),
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

final deviceCommandsRepositoryProvider = Provider<DeviceCommandsRepository>((
  ref,
) {
  final repo = DeviceCommandsRepositoryImpl(
    DeviceCommandsRemoteDataSource(ref.watch(webDioProvider)),
    epoch: ref.watch(sessionEpochProvider),
  );
  ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
    if (historyCacheMustClear(
      wasAuthenticated: previous?.isAuthenticated == true,
      isAuthenticated: next.isAuthenticated,
      previousUserId: previous?.user?.id,
      nextUserId: next.user?.id,
    )) {
      repo.clearCache();
    }
  });
  return repo;
});

final deviceAlarmEventsRepositoryProvider =
    Provider<DeviceAlarmEventsRepository>((ref) {
      final repo = DeviceAlarmEventsRepositoryImpl(
        DeviceAlarmEventsRemoteDataSource(ref.watch(webDioProvider)),
        epoch: ref.watch(sessionEpochProvider),
      );
      ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
        if (historyCacheMustClear(
          wasAuthenticated: previous?.isAuthenticated == true,
          isAuthenticated: next.isAuthenticated,
          previousUserId: previous?.user?.id,
          nextUserId: next.user?.id,
        )) {
          repo.clearCache();
        }
      });
      return repo;
    });

final listDeviceAlarmEventsUseCaseProvider =
    Provider<ListDeviceAlarmEventsUseCase>((ref) {
      return ListDeviceAlarmEventsUseCase(
        ref.watch(deviceAlarmEventsRepositoryProvider),
      );
    });

final getDeviceAlarmEventUseCaseProvider = Provider<GetDeviceAlarmEventUseCase>(
  (ref) {
    return GetDeviceAlarmEventUseCase(
      ref.watch(deviceAlarmEventsRepositoryProvider),
    );
  },
);

final listDeviceCommandsUseCaseProvider = Provider<ListDeviceCommandsUseCase>((
  ref,
) {
  return ListDeviceCommandsUseCase(ref.watch(deviceCommandsRepositoryProvider));
});

final getDeviceCommandUseCaseProvider = Provider<GetDeviceCommandUseCase>((
  ref,
) {
  return GetDeviceCommandUseCase(ref.watch(deviceCommandsRepositoryProvider));
});

final createDeviceCommandUseCaseProvider = Provider<CreateDeviceCommandUseCase>(
  (ref) {
    return CreateDeviceCommandUseCase(
      ref.watch(deviceCommandsRepositoryProvider),
    );
  },
);
