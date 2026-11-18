import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

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
