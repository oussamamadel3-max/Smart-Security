import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../admin_ownership_view.dart';
import 'admin_device_indicators_entity.dart';

/// Read model from `GET api/admin/devices`.
class AdminDeviceEntity {
  const AdminDeviceEntity({
    required this.deviceId,
    required this.hardwareUid,
    required this.displayName,
    required this.lifecycleStatus,
    required this.currentOwnershipVersion,
    required this.confirmedOwnershipVersion,
    required this.awaitingOwnershipAck,
    required this.indicators,
    this.ownershipId,
    this.ownershipStatus,
    this.ownerUserId,
    this.ownerUserName,
    this.ownerFullName,
    this.ownershipVersion,
  });

  final String deviceId;
  final String hardwareUid;
  final String displayName;
  final String lifecycleStatus;
  final int currentOwnershipVersion;
  final String? ownershipId;
  final OwnershipStatus? ownershipStatus;
  final String? ownerUserId;
  final String? ownerUserName;
  final String? ownerFullName;
  final int? ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool awaitingOwnershipAck;
  final AdminDeviceIndicatorsEntity indicators;

  bool get isRetired => lifecycleStatus.toUpperCase() == 'RETIRED';

  AdminOwnershipKind get ownershipKind => AdminOwnershipView.classify(
    status: ownershipStatus,
    awaitingOwnershipAck: awaitingOwnershipAck,
  );

  bool get hasOpenOwnership =>
      ownershipStatus == OwnershipStatus.pending ||
      ownershipStatus == OwnershipStatus.active;

  bool get canOfferLink => !isRetired && !hasOpenOwnership;

  bool get canOfferTransfer => !isRetired && hasOpenOwnership;

  bool get canOfferUnlink => !isRetired && hasOpenOwnership;

  bool get canOfferArchive => !isRetired;

  String get ownerLabel {
    final name = ownerFullName?.trim();
    if (name != null && name.isNotEmpty) return name;
    final user = ownerUserName?.trim();
    if (user != null && user.isNotEmpty) return user;
    return '—';
  }
}
