import 'package:smart_security_app_core/smart_security_app_core.dart';

/// How an admin screen labels ownership. Pending is never shown as active.
enum AdminOwnershipKind {
  unlinked,
  pending,
  active,
  activeAwaitingAck,
  closed,
  unknown,
}

abstract final class AdminOwnershipView {
  /// Admin consoles do not expose owner ARM/DISARM/MUTE/TEST controls.
  static const exposesOperationalCommands = false;

  static AdminOwnershipKind classify({
    OwnershipStatus? status,
    required bool awaitingOwnershipAck,
  }) {
    if (status == null) return AdminOwnershipKind.unlinked;
    if (status.isLinkPending) return AdminOwnershipKind.pending;
    if (status == OwnershipStatus.active && awaitingOwnershipAck) {
      return AdminOwnershipKind.activeAwaitingAck;
    }
    if (status == OwnershipStatus.active) return AdminOwnershipKind.active;
    if (status == OwnershipStatus.ended ||
        status == OwnershipStatus.cancelled) {
      return AdminOwnershipKind.closed;
    }
    return AdminOwnershipKind.unknown;
  }
}
