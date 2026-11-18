import 'package:smart_security_app_core/smart_security_app_core.dart';

/// `POST api/admin/devices` response. Secrets are never included.
class RegisteredDeviceEntity {
  const RegisteredDeviceEntity({
    required this.id,
    required this.hardwareUid,
    required this.displayName,
    required this.lifecycleStatus,
    required this.currentOwnershipVersion,
    required this.settingsVersion,
    required this.credentialAuthType,
    required this.hasActiveCredential,
    required this.hasApprovedSettings,
    required this.createdAt,
  });

  final String id;
  final String hardwareUid;
  final String displayName;
  final String lifecycleStatus;
  final int currentOwnershipVersion;
  final int settingsVersion;
  final String credentialAuthType;
  final bool hasActiveCredential;
  final bool hasApprovedSettings;
  final DateTime createdAt;
}

/// Initial link result. [ownershipStatus] is expected to be PENDING.
class LinkedDeviceEntity {
  const LinkedDeviceEntity({
    required this.deviceId,
    required this.displayName,
    required this.ownershipStatus,
    required this.ownershipVersion,
    required this.confirmedOwnershipVersion,
    required this.canControl,
    required this.ownershipSyncQueued,
    required this.linkedAt,
  });

  final String deviceId;
  final String displayName;
  final OwnershipStatus ownershipStatus;
  final int ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool canControl;
  final bool ownershipSyncQueued;
  final DateTime linkedAt;

  bool get isPending => ownershipStatus.isLinkPending;
}

/// Transfer result. ACTIVE on the server is not a device ACK.
class TransferredDeviceEntity {
  const TransferredDeviceEntity({
    required this.deviceId,
    required this.displayName,
    required this.ownershipStatus,
    required this.ownershipVersion,
    required this.confirmedOwnershipVersion,
    required this.awaitingOwnershipAck,
    required this.canPublishCommands,
    required this.ownershipSyncQueued,
    required this.transferredAt,
    required this.cancelledCommands,
    required this.cancelledNotifications,
  });

  final String deviceId;
  final String displayName;
  final OwnershipStatus ownershipStatus;
  final int ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool awaitingOwnershipAck;
  final bool canPublishCommands;
  final bool ownershipSyncQueued;
  final DateTime transferredAt;
  final int cancelledCommands;
  final int cancelledNotifications;
}

class UnlinkedDeviceEntity {
  const UnlinkedDeviceEntity({
    required this.deviceId,
    required this.displayName,
    required this.closedOwnershipStatus,
    required this.ownershipVersion,
    required this.confirmedOwnershipVersion,
    required this.ownershipSyncQueued,
    required this.unlinkedAt,
  });

  final String deviceId;
  final String displayName;
  final OwnershipStatus closedOwnershipStatus;
  final int ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool ownershipSyncQueued;
  final DateTime unlinkedAt;
}

class ArchivedDeviceEntity {
  const ArchivedDeviceEntity({
    required this.id,
    required this.hardwareUid,
    required this.displayName,
    required this.lifecycleStatus,
    required this.retiredAt,
    required this.ownershipEnded,
    required this.ownershipSyncQueued,
    required this.credentialsRevoked,
    required this.hasActiveCredential,
    required this.brokerAccessRevoked,
  });

  final String id;
  final String hardwareUid;
  final String displayName;
  final String lifecycleStatus;
  final DateTime retiredAt;
  final bool ownershipEnded;
  final bool ownershipSyncQueued;
  final int credentialsRevoked;
  final bool hasActiveCredential;
  final bool brokerAccessRevoked;
}
