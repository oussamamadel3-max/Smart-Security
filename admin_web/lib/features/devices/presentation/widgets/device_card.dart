import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/owner_device_entity.dart';
import 'device_status_pills.dart';

class DeviceCard extends StatelessWidget {
  const DeviceCard({super.key, required this.device, this.onTap});

  final OwnerDeviceEntity device;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final theme = Theme.of(context);
    final indicators = device.indicators;

    return Material(
      color: DesignTokens.card,
      borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
      child: InkWell(
        onTap: device.isInitialLinkPending ? null : onTap,
        borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
        hoverColor: DesignTokens.sky,
        child: Ink(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(DesignTokens.radiusLg),
            border: Border.all(color: DesignTokens.border),
            boxShadow: DesignTokens.cardShadow,
          ),
          child: Padding(
            padding: const EdgeInsets.all(DesignTokens.spaceLg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      width: 41,
                      height: 41,
                      decoration: BoxDecoration(
                        color: DesignTokens.sky,
                        borderRadius: BorderRadius.circular(
                          DesignTokens.radiusMd,
                        ),
                      ),
                      alignment: Alignment.center,
                      child: const Text('⌂', style: TextStyle(fontSize: 21)),
                    ),
                    const Spacer(),
                    if (device.showsOperationalIndicators)
                      DeviceStatusLabels.connection(
                        context,
                        indicators!.connection,
                      )
                    else
                      DeviceStatusLabels.ownership(context, device),
                  ],
                ),
                const SizedBox(height: DesignTokens.spaceMd),
                Text(
                  device.displayName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                    color: DesignTokens.ink,
                  ),
                ),
                const SizedBox(height: DesignTokens.spaceXs),
                Text(
                  device.linkStatusMessage.isEmpty
                      ? (device.isInitialLinkPending
                            ? tr.linkPendingTitle
                            : tr.ownershipActive)
                      : device.linkStatusMessage,
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: DesignTokens.muted,
                  ),
                ),
                if (device.showsOperationalIndicators &&
                    indicators != null) ...[
                  const SizedBox(height: DesignTokens.spaceMd),
                  Wrap(
                    spacing: DesignTokens.spaceSm,
                    runSpacing: DesignTokens.spaceSm,
                    children: [
                      DeviceStatusLabels.arm(context, indicators.arm),
                      DeviceStatusLabels.health(context, indicators.health),
                      DeviceStatusLabels.siren(context, indicators.siren),
                    ],
                  ),
                  if (indicators.showsStaleWarning) ...[
                    const SizedBox(height: DesignTokens.spaceSm),
                    Text(
                      tr.staleWhileOffline,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: DesignTokens.warningSoftText,
                      ),
                    ),
                  ],
                  if (indicators.lastSeenAt != null) ...[
                    const SizedBox(height: DesignTokens.spaceMd),
                    Text(
                      '${tr.lastSeen}: ${_formatRelative(indicators.lastSeenAt!)}',
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: DesignTokens.muted,
                      ),
                    ),
                  ],
                  if (device.isActiveAwaitingOwnershipSync) ...[
                    const SizedBox(height: DesignTokens.spaceSm),
                    Text(
                      tr.commandHoldUntilSync,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: DesignTokens.warningSoftText,
                      ),
                    ),
                  ],
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  static String _formatRelative(DateTime time) {
    final diff = DateTime.now().difference(time);
    if (diff.inSeconds < 60) return 'قبل ${diff.inSeconds} ثانية';
    if (diff.inMinutes < 60) return 'قبل ${diff.inMinutes} دقيقة';
    if (diff.inHours < 24) return 'قبل ${diff.inHours} ساعة';
    return 'قبل ${diff.inDays} يوم';
  }
}
