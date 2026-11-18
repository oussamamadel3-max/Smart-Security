import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/domain/device_status_codes.dart';
import 'package:security_mobile/features/devices/domain/command_poll_policy.dart';
import 'package:security_mobile/features/devices/domain/entities/device_command_entity.dart';

DeviceCommandEntity _cmd({
  required CommandStatus status,
  DateTime? reconciledAt,
}) {
  return DeviceCommandEntity(
    commandId: 'c1',
    deviceId: 'd1',
    type: CommandType.arm,
    status: status,
    commandSequence: 1,
    ownershipVersion: 1,
    observedStateSequence: 0,
    createdAt: DateTime.now(),
    expiresAt: DateTime.now(),
    actualStateChanged: false,
    idempotentReplay: false,
    reconciledAt: reconciledAt,
  );
}

void main() {
  test('poll stops at max attempts without inventing terminal success', () {
    final pending = _cmd(status: CommandStatus.pending);
    expect(
      CommandPollPolicy.shouldContinue(
        attempt: CommandPollPolicy.maxAttempts,
        consecutiveFailures: 0,
        latest: pending,
      ),
      isFalse,
    );
    expect(
      CommandPollPolicy.pollStopClearsConflictBlock(pending),
      isTrue,
    );
  });

  test('TIMED_OUT keeps conflict block until reconciledAt', () {
    final timedOut = _cmd(status: CommandStatus.timedOut);
    expect(timedOut.blocksConflictingUntilReconciled, isTrue);
    expect(
      CommandPollPolicy.pollStopClearsConflictBlock(timedOut),
      isFalse,
    );

    final reconciled = _cmd(
      status: CommandStatus.timedOut,
      reconciledAt: DateTime.now(),
    );
    expect(CommandPollPolicy.isReconciled(reconciled), isTrue);
    expect(reconciled.blocksConflictingUntilReconciled, isFalse);
    expect(
      CommandPollPolicy.pollStopClearsConflictBlock(reconciled),
      isTrue,
    );
  });

  test('consecutive poll failures stop polling', () {
    expect(
      CommandPollPolicy.shouldContinue(
        attempt: 3,
        consecutiveFailures: CommandPollPolicy.maxConsecutiveFailures,
        latest: _cmd(status: CommandStatus.pending),
      ),
      isFalse,
    );
  });
}
