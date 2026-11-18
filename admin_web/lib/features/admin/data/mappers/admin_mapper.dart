import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/admin_device_entity.dart';
import '../../domain/entities/admin_device_indicators_entity.dart';
import '../../domain/entities/admin_mutation_entities.dart';
import '../../domain/entities/owner_account_entity.dart';
import 'admin_json.dart';

abstract final class OwnerAccountMapper {
  static OwnerAccountEntity toEntity(Map<String, dynamic> json) {
    return OwnerAccountEntity(
      id: readString(json['id']),
      userName: readOptionalString(json['userName']),
      fullName: readString(json['fullName']),
      email: readOptionalString(json['email']),
      isActive: readBool(json['isActive']),
      deletedAt: readDateTime(json['deletedAt']),
      createdAt: readRequiredDateTime(json['createdAt']),
      updatedAt: readDateTime(json['updatedAt']),
    );
  }
}

abstract final class AdminDeviceMapper {
  static AdminDeviceEntity toEntity(Map<String, dynamic> json) {
    final indicators = readMap(json['indicators']);
    final rawStatus = readOptionalString(json['ownershipStatus']);
    return AdminDeviceEntity(
      deviceId: readString(json['deviceId']),
      hardwareUid: readString(json['hardwareUid']),
      displayName: readString(json['displayName']),
      lifecycleStatus: readString(json['lifecycleStatus']),
      currentOwnershipVersion: readInt(json['currentOwnershipVersion']),
      ownershipId: readOptionalString(json['ownershipId']),
      ownershipStatus: rawStatus == null
          ? null
          : OwnershipStatus.parse(rawStatus),
      ownerUserId: readOptionalString(json['ownerUserId']),
      ownerUserName: readOptionalString(json['ownerUserName']),
      ownerFullName: readOptionalString(json['ownerFullName']),
      ownershipVersion: json['ownershipVersion'] == null
          ? null
          : readInt(json['ownershipVersion']),
      confirmedOwnershipVersion: readInt(json['confirmedOwnershipVersion']),
      awaitingOwnershipAck: readBool(json['awaitingOwnershipAck']),
      indicators: AdminDeviceIndicatorsEntity(
        connection: ConnectionStatus.parse(
          readOptionalString(indicators['connectionStatus']),
        ),
        arm: ArmStatus.parse(readOptionalString(indicators['armState'])),
        siren: SirenStatus.parse(readOptionalString(indicators['sirenState'])),
        health: DiagnosticStatus.parse(
          readOptionalString(indicators['healthState']),
        ),
        healthCode: readOptionalString(indicators['healthCode']),
        lastSeenAt: readDateTime(indicators['lastSeenAt']),
        lastStateAt: readDateTime(indicators['lastStateAt']),
        stateSequence: readInt(indicators['stateSequence']),
        clockTrusted: readBool(indicators['clockTrusted']),
        powerSource: readOptionalString(indicators['powerSource']),
        batteryPercent: indicators['batteryPercent'] == null
            ? null
            : readInt(indicators['batteryPercent']),
        heartbeatTimeoutSeconds: readInt(indicators['heartbeatTimeoutSeconds']),
      ),
    );
  }

  static RegisteredDeviceEntity registered(Map<String, dynamic> json) {
    return RegisteredDeviceEntity(
      id: readString(json['id']),
      hardwareUid: readString(json['hardwareUid']),
      displayName: readString(json['displayName']),
      lifecycleStatus: readString(json['lifecycleStatus']),
      currentOwnershipVersion: readInt(json['currentOwnershipVersion']),
      settingsVersion: readInt(json['settingsVersion']),
      credentialAuthType: readString(json['credentialAuthType']),
      hasActiveCredential: readBool(json['hasActiveCredential']),
      hasApprovedSettings: readBool(json['hasApprovedSettings']),
      createdAt: readRequiredDateTime(json['createdAt']),
    );
  }

  static LinkedDeviceEntity linked(Map<String, dynamic> json) {
    return LinkedDeviceEntity(
      deviceId: readString(json['deviceId']),
      displayName: readString(json['displayName']),
      ownershipStatus: OwnershipStatus.parse(
        readOptionalString(json['ownershipStatus']),
      ),
      ownershipVersion: readInt(json['ownershipVersion']),
      confirmedOwnershipVersion: readInt(json['confirmedOwnershipVersion']),
      canControl: readBool(json['canControl']),
      ownershipSyncQueued: readBool(json['ownershipSyncQueued']),
      linkedAt: readRequiredDateTime(json['linkedAt']),
    );
  }

  static TransferredDeviceEntity transferred(Map<String, dynamic> json) {
    return TransferredDeviceEntity(
      deviceId: readString(json['deviceId']),
      displayName: readString(json['displayName']),
      ownershipStatus: OwnershipStatus.parse(
        readOptionalString(json['ownershipStatus']),
      ),
      ownershipVersion: readInt(json['ownershipVersion']),
      confirmedOwnershipVersion: readInt(json['confirmedOwnershipVersion']),
      awaitingOwnershipAck: readBool(json['awaitingOwnershipAck']),
      canPublishCommands: readBool(json['canPublishCommands']),
      ownershipSyncQueued: readBool(json['ownershipSyncQueued']),
      transferredAt: readRequiredDateTime(json['transferredAt']),
      cancelledCommands: readInt(json['cancelledCommands']),
      cancelledNotifications: readInt(json['cancelledNotifications']),
    );
  }

  static UnlinkedDeviceEntity unlinked(Map<String, dynamic> json) {
    return UnlinkedDeviceEntity(
      deviceId: readString(json['deviceId']),
      displayName: readString(json['displayName']),
      closedOwnershipStatus: OwnershipStatus.parse(
        readOptionalString(json['closedOwnershipStatus']),
      ),
      ownershipVersion: readInt(json['ownershipVersion']),
      confirmedOwnershipVersion: readInt(json['confirmedOwnershipVersion']),
      ownershipSyncQueued: readBool(json['ownershipSyncQueued']),
      unlinkedAt: readRequiredDateTime(json['unlinkedAt']),
    );
  }

  static ArchivedDeviceEntity archived(Map<String, dynamic> json) {
    return ArchivedDeviceEntity(
      id: readString(json['id']),
      hardwareUid: readString(json['hardwareUid']),
      displayName: readString(json['displayName']),
      lifecycleStatus: readString(json['lifecycleStatus']),
      retiredAt: readRequiredDateTime(json['retiredAt']),
      ownershipEnded: readBool(json['ownershipEnded']),
      ownershipSyncQueued: readBool(json['ownershipSyncQueued']),
      credentialsRevoked: readInt(json['credentialsRevoked']),
      hasActiveCredential: readBool(json['hasActiveCredential']),
      brokerAccessRevoked: readBool(json['brokerAccessRevoked']),
    );
  }
}
