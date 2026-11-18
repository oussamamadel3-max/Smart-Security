import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/widgets/owner_visual.dart';
import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/entities/device_indicators_entity.dart';
import '../../domain/entities/owner_device_entity.dart';

class StatusPill extends StatelessWidget {
  const StatusPill({
    super.key,
    required this.label,
    this.tone = StatusPillTone.gray,
  });

  final String label;
  final StatusPillTone tone;

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = switch (tone) {
      StatusPillTone.green => (
        DesignTokens.successSoftBg,
        DesignTokens.successSoftText,
      ),
      StatusPillTone.red => (
        DesignTokens.dangerSoftBg,
        DesignTokens.dangerSoftText,
      ),
      StatusPillTone.blue => (
        DesignTokens.infoSoftBg,
        DesignTokens.infoSoftText,
      ),
      StatusPillTone.amber => (
        DesignTokens.warningSoftBg,
        DesignTokens.warningSoftText,
      ),
      StatusPillTone.gray => (
        AppColors.lightSurfaceVariant,
        DesignTokens.muted,
      ),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(DesignTokens.radiusPill),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
          color: fg,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

enum StatusPillTone { green, red, blue, amber, gray }

abstract final class DeviceStatusLabels {
  static StatusPill connection(BuildContext context, ConnectionStatus status) {
    final tr = context.tr;
    return switch (status) {
      ConnectionStatus.online => StatusPill(
        label: tr.connectionOnline,
        tone: StatusPillTone.green,
      ),
      ConnectionStatus.offline => StatusPill(
        label: tr.connectionOffline,
        tone: StatusPillTone.red,
      ),
      ConnectionStatus.unknown => StatusPill(
        label: tr.connectionOffline,
        tone: StatusPillTone.gray,
      ),
    };
  }

  static StatusPill arm(BuildContext context, ArmStatus status) {
    final tr = context.tr;
    return switch (status) {
      ArmStatus.armed => StatusPill(
        label: tr.armArmed,
        tone: StatusPillTone.blue,
      ),
      ArmStatus.disarmed => StatusPill(
        label: tr.armDisarmed,
        tone: StatusPillTone.gray,
      ),
      ArmStatus.unknown => StatusPill(
        label: tr.armUnknown,
        tone: StatusPillTone.gray,
      ),
    };
  }

  static StatusPill health(BuildContext context, DiagnosticStatus status) {
    final tr = context.tr;
    return switch (status) {
      DiagnosticStatus.healthy => StatusPill(
        label: tr.healthHealthy,
        tone: StatusPillTone.green,
      ),
      DiagnosticStatus.fault => StatusPill(
        label: tr.healthFault,
        tone: StatusPillTone.red,
      ),
      DiagnosticStatus.unknown => StatusPill(
        label: tr.healthUnknown,
        tone: StatusPillTone.gray,
      ),
    };
  }

  static StatusPill siren(BuildContext context, SirenStatus status) {
    final tr = context.tr;
    return switch (status) {
      SirenStatus.off => StatusPill(
        label: tr.sirenOff,
        tone: StatusPillTone.gray,
      ),
      SirenStatus.alarm => StatusPill(
        label: tr.sirenAlarm,
        tone: StatusPillTone.red,
      ),
      SirenStatus.test => StatusPill(
        label: tr.sirenTest,
        tone: StatusPillTone.amber,
      ),
      SirenStatus.unknown => StatusPill(
        label: tr.sirenUnknown,
        tone: StatusPillTone.gray,
      ),
    };
  }

  static List<Widget> indicatorPills(
    BuildContext context,
    DeviceIndicatorsEntity indicators,
  ) {
    return [
      connection(context, indicators.connection),
      arm(context, indicators.arm),
      siren(context, indicators.siren),
      health(context, indicators.health),
    ];
  }

  static StatusPill ownership(BuildContext context, OwnerDeviceEntity device) {
    final tr = context.tr;
    if (device.isInitialLinkPending) {
      return StatusPill(label: tr.linkPendingTitle, tone: StatusPillTone.amber);
    }
    if (device.isActiveAwaitingOwnershipSync) {
      return StatusPill(
        label: tr.awaitingOwnershipSync,
        tone: StatusPillTone.amber,
      );
    }
    return StatusPill(label: tr.ownershipActive, tone: StatusPillTone.green);
  }

  static StatusPill commandStatus(CommandStatus status) {
    return switch (status) {
      CommandStatus.executed => const StatusPill(
        label: OwnerVisualCopy.executed,
        tone: StatusPillTone.green,
      ),
      CommandStatus.pending => const StatusPill(
        label: OwnerVisualCopy.pending,
        tone: StatusPillTone.amber,
      ),
      CommandStatus.timedOut => const StatusPill(
        label: OwnerVisualCopy.timedOut,
        tone: StatusPillTone.red,
      ),
      CommandStatus.rejected => const StatusPill(
        label: OwnerVisualCopy.rejected,
        tone: StatusPillTone.red,
      ),
      CommandStatus.cancelled => const StatusPill(
        label: OwnerVisualCopy.cancelled,
        tone: StatusPillTone.gray,
      ),
      CommandStatus.unknown => StatusPill(
        label: status.code,
        tone: StatusPillTone.gray,
      ),
    };
  }

  static StatusPill eventClass(BuildContext context, AlarmEventEntity event) {
    final tr = context.tr;
    if (event.showBufferedNotice) {
      return StatusPill(label: tr.bufferedLabel, tone: StatusPillTone.amber);
    }
    return switch (event.status) {
      AlarmEventStatus.ended => const StatusPill(
        label: OwnerVisualCopy.eventEnded,
        tone: StatusPillTone.green,
      ),
      AlarmEventStatus.active => const StatusPill(
        label: OwnerVisualCopy.eventActive,
        tone: StatusPillTone.red,
      ),
      AlarmEventStatus.unknown => StatusPill(
        label: event.status.code,
        tone: StatusPillTone.gray,
      ),
    };
  }
}
