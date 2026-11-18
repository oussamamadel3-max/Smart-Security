import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/domain/device_status_codes.dart';

void main() {
  test('unknown wire values never parse as success states', () {
    expect(OwnershipStatus.parse('WEIRD'), OwnershipStatus.unknown);
    expect(ArmStatus.parse(null), ArmStatus.unknown);
    expect(CommandStatus.parse('DONE'), CommandStatus.unknown);
    expect(CommandStatus.pending.isConfirmedExecution, isFalse);
    expect(CommandStatus.timedOut.isUnresolvedTimeout, isTrue);
    expect(CommandStatus.executed.isConfirmedExecution, isTrue);
  });
}
