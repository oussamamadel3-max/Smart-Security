import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'device_indicators_entity.dart';

class OwnerDeviceEntity {
  const OwnerDeviceEntity({
    required this.deviceId,
    required this.displayName,
    required this.ownershipStatus,
    required this.ownershipVersion,
    required this.confirmedOwnershipVersion,
    required this.awaitingOwnershipAck,
    required this.canControl,
    required this.linkStatusMessage,
    this.indicators,
    this.deviceLifecycleStatus,
  });

  final String deviceId;
  final String displayName;
  final OwnershipStatus ownershipStatus;
  final int ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool awaitingOwnershipAck;
  final bool canControl;
  final String linkStatusMessage;
  final DeviceIndicatorsEntity? indicators;
  final String? deviceLifecycleStatus;

  bool get isInitialLinkPending => ownershipStatus == OwnershipStatus.pending;

  /// Transfer / ACTIVE ownership waiting for device ownership-version ack.
  bool get isActiveAwaitingOwnershipSync =>
      ownershipStatus == OwnershipStatus.active && awaitingOwnershipAck;

  bool get isFullyActive =>
      ownershipStatus == OwnershipStatus.active && !awaitingOwnershipAck;

  /// PENDING must not expose operational indicators (FR-035).
  bool get showsLimitedPendingView =>
      isInitialLinkPending || indicators == null;

  bool get showsOperationalIndicators =>
      ownershipStatus == OwnershipStatus.active && indicators != null;
}
