import 'entities/device_command_entity.dart';

/// Client-side polling policy for command status (not a G1 contract).
///
/// Stopping the poll must never invent EXECUTED/REJECTED and must never lift
/// TIMED_OUT conflict blocking without server `reconciledAt`.
abstract final class CommandPollPolicy {
  static const interval = Duration(seconds: 2);
  static const maxAttempts = 30; // ~60s wall time
  static const maxConsecutiveFailures = 3;

  /// Whether another GET poll should be scheduled.
  static bool shouldContinue({
    required int attempt,
    required int consecutiveFailures,
    required DeviceCommandEntity? latest,
  }) {
    if (attempt >= maxAttempts) return false;
    if (consecutiveFailures >= maxConsecutiveFailures) return false;
    if (latest == null) return true;
    if (latest.isTerminal) {
      // TIMED_OUT stays terminal for poll stop, but conflict block remains.
      return false;
    }
    return true;
  }

  /// Poll stop alone does not mean the command finished successfully.
  static bool pollStopClearsConflictBlock(DeviceCommandEntity? command) {
    if (command == null) return false;
    return !command.blocksConflictingUntilReconciled;
  }

  /// Reconciliation available from server fields without G1 ACK payload tests.
  static bool isReconciled(DeviceCommandEntity command) =>
      command.reconciledAt != null;
}
