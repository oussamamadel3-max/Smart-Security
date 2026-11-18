import 'dart:async';
import 'dart:math';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/domain/device_status_codes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../domain/command_poll_policy.dart';
import '../../domain/entities/device_command_entity.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../providers/devices_providers.dart';

class DeviceDetailState {
  const DeviceDetailState({
    this.device,
    this.isLoading = false,
    this.isSubmitting = false,
    this.pendingCommand,
    this.pollStoppedWithoutTerminal = false,
    this.error,
    this.infoMessage,
  });

  final OwnerDeviceEntity? device;
  final bool isLoading;
  final bool isSubmitting;
  final DeviceCommandEntity? pendingCommand;

  /// True when client stopped GET polling — not proof of command completion.
  final bool pollStoppedWithoutTerminal;
  final ErrorState? error;
  final String? infoMessage;

  bool get hasUnresolvedTimeout =>
      pendingCommand?.blocksConflictingUntilReconciled == true;

  DeviceDetailState copyWith({
    OwnerDeviceEntity? device,
    bool? isLoading,
    bool? isSubmitting,
    DeviceCommandEntity? pendingCommand,
    bool clearPending = false,
    bool? pollStoppedWithoutTerminal,
    ErrorState? error,
    bool clearError = false,
    String? infoMessage,
    bool clearInfo = false,
  }) {
    return DeviceDetailState(
      device: device ?? this.device,
      isLoading: isLoading ?? this.isLoading,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      pendingCommand:
          clearPending ? null : (pendingCommand ?? this.pendingCommand),
      pollStoppedWithoutTerminal:
          pollStoppedWithoutTerminal ?? this.pollStoppedWithoutTerminal,
      error: clearError ? null : (error ?? this.error),
      infoMessage: clearInfo ? null : (infoMessage ?? this.infoMessage),
    );
  }
}

class DeviceDetailViewModel extends FamilyNotifier<DeviceDetailState, String> {
  Timer? _pollTimer;
  int _pollAttempt = 0;
  int _pollFailures = 0;

  @override
  DeviceDetailState build(String arg) {
    ref.onDispose(() => _stopPolling());
    Future.microtask(() => load(arg));
    return const DeviceDetailState(isLoading: true);
  }

  void _stopPolling() {
    _pollTimer?.cancel();
    _pollTimer = null;
  }

  static String _newIdempotencyKey() {
    final r = Random.secure();
    final bytes = List<int>.generate(16, (_) => r.nextInt(256));
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    String hex(int b) => b.toRadixString(16).padLeft(2, '0');
    final s = bytes.map(hex).join();
    return '${s.substring(0, 8)}-${s.substring(8, 12)}-'
        '${s.substring(12, 16)}-${s.substring(16, 20)}-${s.substring(20)}';
  }

  Future<void> load(String deviceId) async {
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const DeviceDetailState();
      return;
    }

    state = state.copyWith(isLoading: true, clearError: true, clearInfo: true);
    final result = await ref.read(getMyDeviceUseCaseProvider)(deviceId);
    result.fold(
      onFailure: (failure) {
        if (failure.requiresReauthentication ||
            failure is ForbiddenFailure ||
            failure is NotFoundFailure) {
          ref.read(devicesRepositoryProvider).clearCache();
        }
        state = DeviceDetailState(
          error: FailureHandler.handle(failure, onRetry: () => load(deviceId)),
          pendingCommand: state.pendingCommand,
        );
      },
      onSuccess: (device) {
        state = state.copyWith(
          device: device,
          isLoading: false,
          clearError: true,
        );
      },
    );
  }

  Future<void> submitCommand(CommandType type) async {
    final device = state.device;
    if (device == null || state.isSubmitting) return;

    if (!device.canControl) {
      state = state.copyWith(
        error: ErrorState(
          message: 'التحكم غير متاح حتى اكتمال مزامنة الملكية.',
          displayType: ErrorDisplayType.snackbar,
        ),
      );
      return;
    }

    if (state.hasUnresolvedTimeout) {
      state = state.copyWith(
        error: const ErrorState(
          message:
              'يوجد أمر منتهٍ المهلة بانتظار المصالحة. لا يُرسل أمر متعارض الآن.',
          displayType: ErrorDisplayType.snackbar,
        ),
      );
      return;
    }

    if (type == CommandType.muteAlarm) {
      final alarmId = device.indicators?.currentAlarmEventUid;
      if (alarmId == null || alarmId.isEmpty) {
        state = state.copyWith(
          error: const ErrorState(
            message: 'لا يمكن كتم الصفارة بدون معرّف حادثة جارية من الحالة.',
            displayType: ErrorDisplayType.snackbar,
          ),
        );
        return;
      }
    }

    if (type == CommandType.testSiren &&
        device.indicators?.siren == SirenStatus.alarm) {
      state = state.copyWith(
        error: const ErrorState(
          message: 'لا يُسمح باختبار الصفارة أثناء إنذار نشط.',
          displayType: ErrorDisplayType.snackbar,
        ),
      );
      return;
    }

    if (type == CommandType.stopTest &&
        device.indicators?.siren != SirenStatus.test) {
      state = state.copyWith(
        error: const ErrorState(
          message: 'إيقاف الاختبار متاح فقط أثناء وضع الاختبار.',
          displayType: ErrorDisplayType.snackbar,
        ),
      );
      return;
    }

    state = state.copyWith(isSubmitting: true, clearError: true, clearInfo: true);
    final result = await ref.read(deviceCommandsRepositoryProvider).create(
          deviceId: device.deviceId,
          type: type,
          idempotencyKey: _newIdempotencyKey(),
          targetAlarmEventUid: type == CommandType.muteAlarm
              ? device.indicators?.currentAlarmEventUid
              : null,
        );

    result.fold(
      onFailure: (failure) {
        state = state.copyWith(
          isSubmitting: false,
          error: FailureHandler.handle(failure),
        );
      },
      onSuccess: (command) {
        // HTTP 202 / PENDING must not flip confirmed indicators.
        state = state.copyWith(
          isSubmitting: false,
          pendingCommand: command,
          pollStoppedWithoutTerminal: false,
          infoMessage:
              'تم قبول الطلب بحالة PENDING — بانتظار تأكيد الجهاز. الحالة المؤكدة لم تتغير.',
        );
        _startPolling(device.deviceId, command.commandId);
      },
    );
  }

  void _startPolling(String deviceId, String commandId) {
    _stopPolling();
    _pollAttempt = 0;
    _pollFailures = 0;
    _pollTimer = Timer.periodic(CommandPollPolicy.interval, (_) async {
      _pollAttempt += 1;
      final result = await ref.read(deviceCommandsRepositoryProvider).getById(
            deviceId: deviceId,
            commandId: commandId,
          );
      await result.fold(
        onFailure: (failure) async {
          _pollFailures += 1;
          state = state.copyWith(
            infoMessage:
                'تعذر استعلام حالة الأمر (شبكة/خادم). الاستعلام توقف لا يعني انتهاء الأمر.',
          );
          if (!CommandPollPolicy.shouldContinue(
            attempt: _pollAttempt,
            consecutiveFailures: _pollFailures,
            latest: state.pendingCommand,
          )) {
            _stopPolling();
            state = state.copyWith(
              pollStoppedWithoutTerminal: true,
              infoMessage:
                  'توقف استعلام حالة الأمر. القيود تبقى وفق آخر حالة معروفة من الخادم.',
            );
          }
        },
        onSuccess: (command) async {
          _pollFailures = 0;
          // Keep TIMED_OUT pending for conflict block until reconciledAt.
          final clearPending = command.isTerminal &&
              command.status != CommandStatus.timedOut &&
              CommandPollPolicy.pollStopClearsConflictBlock(command);

          state = state.copyWith(
            pendingCommand: command,
            clearPending: clearPending,
          );

          final continuePoll = CommandPollPolicy.shouldContinue(
            attempt: _pollAttempt,
            consecutiveFailures: _pollFailures,
            latest: command,
          );
          if (!continuePoll) {
            _stopPolling();
            if (command.isTerminal) {
              await load(deviceId);
              if (command.status == CommandStatus.timedOut) {
                final reconciled = CommandPollPolicy.isReconciled(command);
                state = state.copyWith(
                  pendingCommand: command,
                  clearPending: reconciled,
                  pollStoppedWithoutTerminal: false,
                  infoMessage: reconciled
                      ? 'تمت مصالحة الأمر المنتهي المهلة.'
                      : 'انتهت مهلة الأمر (TIMED_OUT) — النتيجة غير مؤكدة حتى المصالحة.',
                );
              } else if (command.status == CommandStatus.executed) {
                state = state.copyWith(
                  clearPending: true,
                  pollStoppedWithoutTerminal: false,
                  infoMessage:
                      'تم تأكيد التنفيذ من الجهاز. تم تحديث الحالة المؤكدة.',
                );
              } else if (command.status == CommandStatus.rejected) {
                state = state.copyWith(
                  clearPending: true,
                  pollStoppedWithoutTerminal: false,
                  infoMessage:
                      'رُفض الأمر${command.rejectCode == null ? '' : ': ${command.rejectCode}'}.',
                );
              }
            } else {
              state = state.copyWith(
                pollStoppedWithoutTerminal: true,
                pendingCommand: command,
                infoMessage:
                    'توقف استعلام حالة الأمر دون حالة نهائية. لا يُفترض التنفيذ ولا تُرفع القيود.',
              );
            }
          }
        },
      );
    });
  }
}

final deviceDetailViewModelProvider = NotifierProvider.family<
    DeviceDetailViewModel, DeviceDetailState, String>(
  DeviceDetailViewModel.new,
);
