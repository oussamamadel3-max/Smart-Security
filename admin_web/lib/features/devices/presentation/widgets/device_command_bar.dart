import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Owner command row mapped from `.command-row` in the device detail design.
class DeviceCommandBar extends StatelessWidget {
  const DeviceCommandBar({
    super.key,
    required this.enabled,
    required this.submitting,
    required this.onArm,
    required this.onDisarm,
    required this.onMute,
    required this.onTest,
    required this.onStopTest,
    required this.muteEnabled,
    required this.testEnabled,
    required this.stopTestEnabled,
  });

  final bool enabled;
  final bool submitting;
  final VoidCallback onArm;
  final VoidCallback onDisarm;
  final VoidCallback onMute;
  final VoidCallback onTest;
  final VoidCallback onStopTest;
  final bool muteEnabled;
  final bool testEnabled;
  final bool stopTestEnabled;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final busy = submitting;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: DesignTokens.spaceMd,
          runSpacing: DesignTokens.spaceMd,
          children: [
            ElevatedButton(
              onPressed: enabled && !busy ? onArm : null,
              child: Text('♜  ${tr.commandArm}'),
            ),
            OutlinedButton(
              style: OutlinedButton.styleFrom(
                backgroundColor: DesignTokens.primarySoft,
                foregroundColor: DesignTokens.primaryTextSoft,
                disabledForegroundColor: DesignTokens.primaryTextSoft
                    .withValues(alpha: 0.45),
                side: BorderSide.none,
              ),
              onPressed: enabled && !busy ? onDisarm : null,
              child: Text('♙  ${tr.commandDisarm}'),
            ),
            OutlinedButton(
              onPressed: enabled && muteEnabled && !busy ? onMute : null,
              child: Text('◖  ${tr.commandMute}'),
            ),
            OutlinedButton(
              onPressed: enabled && testEnabled && !busy ? onTest : null,
              child: Text('♫  ${tr.commandTestSiren}'),
            ),
            OutlinedButton(
              onPressed: enabled && stopTestEnabled && !busy
                  ? onStopTest
                  : null,
              child: Text('■  ${tr.commandStopTest}'),
            ),
          ],
        ),
        if (busy)
          const Padding(
            padding: EdgeInsets.only(top: DesignTokens.spaceMd),
            child: Center(child: CircularProgressIndicator()),
          ),
      ],
    );
  }
}
