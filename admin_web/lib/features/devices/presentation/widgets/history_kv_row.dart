import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class HistoryKvRow extends StatelessWidget {
  const HistoryKvRow({super.key, required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: AppColors.lightSurfaceVariant,
        borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              style: theme.textTheme.labelSmall?.copyWith(
                color: DesignTokens.muted,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              value,
              style: theme.textTheme.labelLarge?.copyWith(
                color: DesignTokens.ink,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class HistoryKvGrid extends StatelessWidget {
  const HistoryKvGrid({super.key, required this.rows});

  final List<HistoryKvRow> rows;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final two = constraints.maxWidth >= 560;
        const gap = DesignTokens.spaceMd;
        final width = two
            ? (constraints.maxWidth - gap) / 2
            : constraints.maxWidth;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final row in rows)
              SizedBox(width: width.clamp(0, constraints.maxWidth), child: row),
          ],
        );
      },
    );
  }
}
