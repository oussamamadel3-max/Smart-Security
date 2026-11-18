import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/domain/device_status_codes.dart';
import 'package:security_mobile/features/devices/domain/entities/device_command_entity.dart';

void main() {
  test('PENDING is not confirmed execution', () {
    final command = DeviceCommandEntity(
      commandId: 'c1',
      deviceId: 'd1',
      type: CommandType.arm,
      status: CommandStatus.pending,
      commandSequence: 1,
      ownershipVersion: 1,
      observedStateSequence: 0,
      createdAt: DateTime.now(),
      expiresAt: DateTime.now().add(const Duration(seconds: 30)),
      actualStateChanged: false,
      idempotentReplay: false,
    );

    expect(command.isPendingRequest, isTrue);
    expect(command.status.isConfirmedExecution, isFalse);
    expect(command.actualStateChanged, isFalse);
  });

  test('TIMED_OUT blocks conflicting until reconciled', () {
    final command = DeviceCommandEntity(
      commandId: 'c2',
      deviceId: 'd1',
      type: CommandType.disarm,
      status: CommandStatus.timedOut,
      commandSequence: 2,
      ownershipVersion: 1,
      observedStateSequence: 1,
      createdAt: DateTime.now(),
      expiresAt: DateTime.now(),
      actualStateChanged: false,
      idempotentReplay: false,
    );

    expect(command.blocksConflictingUntilReconciled, isTrue);
    expect(command.status.isUnresolvedTimeout, isTrue);
  });
}
