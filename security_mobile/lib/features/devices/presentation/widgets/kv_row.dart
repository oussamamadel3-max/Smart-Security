import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Label/value row shared by detail screens.
class KvRow extends StatelessWidget {
  const KvRow(this.label, this.value, {super.key, this.labelWidth = 140});

  final String label;
  final String value;
  final double labelWidth;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: DesignTokens.spaceMd),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: labelWidth,
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: DesignTokens.muted,
                  ),
            ),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}
