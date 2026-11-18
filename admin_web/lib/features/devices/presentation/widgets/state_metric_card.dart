import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/widgets/owner_visual.dart';

/// Four-up state cards from `.cards` / `.metric` in `design/index.html`.
class StateMetricGrid extends StatelessWidget {
  const StateMetricGrid({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = OwnerLayout.metricColumns(constraints.maxWidth);
        const gap = DesignTokens.spaceLg;
        final width = (constraints.maxWidth - gap * (columns - 1)) / columns;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final child in children)
              SizedBox(
                width: width.clamp(0, constraints.maxWidth),
                child: child,
              ),
          ],
        );
      },
    );
  }
}

class StateMetricCard extends StatelessWidget {
  const StateMetricCard({
    super.key,
    required this.title,
    required this.value,
    required this.hint,
    required this.symbol,
  });

  final String title;
  final String value;
  final String hint;
  final String symbol;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final compact = value.length > 12;
    return Container(
      constraints: const BoxConstraints(minHeight: 128),
      padding: const EdgeInsets.fromLTRB(20, 19, 20, 19),
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: DesignTokens.border),
        boxShadow: DesignTokens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  title,
                  style: theme.textTheme.labelMedium?.copyWith(
                    color: DesignTokens.muted,
                  ),
                ),
              ),
              Container(
                width: 40,
                height: 40,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: DesignTokens.infoSoftBg,
                  borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
                ),
                child: Text(
                  symbol,
                  style: const TextStyle(
                    fontSize: 20,
                    color: DesignTokens.primary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: DesignTokens.spaceMd),
          Text(
            value,
            style: theme.textTheme.headlineMedium?.copyWith(
              fontSize: compact ? 18 : 28,
              fontWeight: FontWeight.w800,
              height: 1.15,
              color: DesignTokens.ink,
            ),
          ),
          const SizedBox(height: DesignTokens.spaceXs),
          Text(
            hint,
            style: theme.textTheme.labelMedium?.copyWith(
              color: DesignTokens.muted,
            ),
          ),
        ],
      ),
    );
  }
}
