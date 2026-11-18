import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/web_network_providers.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../../../core/session/session_epoch.dart';
import '../../data/datasources/admin_alerts_remote_datasource.dart';
import '../../data/datasources/admin_devices_remote_datasource.dart';
import '../../data/datasources/audit_logs_remote_datasource.dart';
import '../../data/datasources/owner_accounts_remote_datasource.dart';
import '../../data/repositories/admin_alerts_repository_impl.dart';
import '../../data/repositories/admin_devices_repository_impl.dart';
import '../../data/repositories/audit_logs_repository_impl.dart';
import '../../data/repositories/owner_accounts_repository_impl.dart';
import '../../domain/repositories/admin_alerts_repository.dart';
import '../../domain/repositories/admin_devices_repository.dart';
import '../../domain/repositories/audit_logs_repository.dart';
import '../../domain/repositories/owner_accounts_repository.dart';
import '../../domain/usecases/admin_alert_usecases.dart';
import '../../domain/usecases/admin_device_usecases.dart';
import '../../domain/usecases/audit_log_usecases.dart';
import '../../domain/usecases/owner_account_usecases.dart';

final ownerAccountsRepositoryProvider = Provider<OwnerAccountsRepository>((
  ref,
) {
  final repo = OwnerAccountsRepositoryImpl(
    OwnerAccountsRemoteDataSource(ref.watch(webDioProvider)),
    epoch: ref.watch(sessionEpochProvider),
  );
  ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
    if (previous?.isAuthenticated == true && !next.isAuthenticated) {
      repo.clearCache();
    }
    if (previous?.user?.id != null &&
        next.user?.id != null &&
        previous!.user!.id != next.user!.id) {
      repo.clearCache();
    }
  });
  return repo;
});

final adminDevicesRepositoryProvider = Provider<AdminDevicesRepository>((ref) {
  final repo = AdminDevicesRepositoryImpl(
    AdminDevicesRemoteDataSource(ref.watch(webDioProvider)),
    epoch: ref.watch(sessionEpochProvider),
  );
  ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
    if (previous?.isAuthenticated == true && !next.isAuthenticated) {
      repo.clearCache();
    }
    if (previous?.user?.id != null &&
        next.user?.id != null &&
        previous!.user!.id != next.user!.id) {
      repo.clearCache();
    }
  });
  return repo;
});

final listOwnerAccountsUseCaseProvider = Provider<ListOwnerAccountsUseCase>((
  ref,
) {
  return ListOwnerAccountsUseCase(ref.watch(ownerAccountsRepositoryProvider));
});

final getOwnerAccountUseCaseProvider = Provider<GetOwnerAccountUseCase>((ref) {
  return GetOwnerAccountUseCase(ref.watch(ownerAccountsRepositoryProvider));
});

final createOwnerAccountUseCaseProvider = Provider<CreateOwnerAccountUseCase>((
  ref,
) {
  return CreateOwnerAccountUseCase(ref.watch(ownerAccountsRepositoryProvider));
});

final updateOwnerAccountUseCaseProvider = Provider<UpdateOwnerAccountUseCase>((
  ref,
) {
  return UpdateOwnerAccountUseCase(ref.watch(ownerAccountsRepositoryProvider));
});

final disableOwnerAccountUseCaseProvider = Provider<DisableOwnerAccountUseCase>(
  (ref) {
    return DisableOwnerAccountUseCase(
      ref.watch(ownerAccountsRepositoryProvider),
    );
  },
);

final archiveOwnerAccountUseCaseProvider = Provider<ArchiveOwnerAccountUseCase>(
  (ref) {
    return ArchiveOwnerAccountUseCase(
      ref.watch(ownerAccountsRepositoryProvider),
    );
  },
);

final listAdminDevicesUseCaseProvider = Provider<ListAdminDevicesUseCase>((
  ref,
) {
  return ListAdminDevicesUseCase(ref.watch(adminDevicesRepositoryProvider));
});

final getAdminDeviceUseCaseProvider = Provider<GetAdminDeviceUseCase>((ref) {
  return GetAdminDeviceUseCase(ref.watch(adminDevicesRepositoryProvider));
});

final registerAdminDeviceUseCaseProvider = Provider<RegisterAdminDeviceUseCase>(
  (ref) {
    return RegisterAdminDeviceUseCase(
      ref.watch(adminDevicesRepositoryProvider),
    );
  },
);

final linkAdminDeviceUseCaseProvider = Provider<LinkAdminDeviceUseCase>((ref) {
  return LinkAdminDeviceUseCase(ref.watch(adminDevicesRepositoryProvider));
});

final transferAdminDeviceUseCaseProvider = Provider<TransferAdminDeviceUseCase>(
  (ref) {
    return TransferAdminDeviceUseCase(
      ref.watch(adminDevicesRepositoryProvider),
    );
  },
);

final unlinkAdminDeviceUseCaseProvider = Provider<UnlinkAdminDeviceUseCase>((
  ref,
) {
  return UnlinkAdminDeviceUseCase(ref.watch(adminDevicesRepositoryProvider));
});

final archiveAdminDeviceUseCaseProvider = Provider<ArchiveAdminDeviceUseCase>((
  ref,
) {
  return ArchiveAdminDeviceUseCase(ref.watch(adminDevicesRepositoryProvider));
});

final auditLogsRepositoryProvider = Provider<AuditLogsRepository>((ref) {
  return AuditLogsRepositoryImpl(
    AuditLogsRemoteDataSource(ref.watch(webDioProvider)),
  );
});

final adminAlertsRepositoryProvider = Provider<AdminAlertsRepository>((ref) {
  return AdminAlertsRepositoryImpl(
    AdminAlertsRemoteDataSource(ref.watch(webDioProvider)),
  );
});

final queryAuditLogsUseCaseProvider = Provider<QueryAuditLogsUseCase>((ref) {
  return QueryAuditLogsUseCase(ref.watch(auditLogsRepositoryProvider));
});

final listAdminAlertsUseCaseProvider = Provider<ListAdminAlertsUseCase>((ref) {
  return ListAdminAlertsUseCase(ref.watch(adminAlertsRepositoryProvider));
});

final getAdminAlertUseCaseProvider = Provider<GetAdminAlertUseCase>((ref) {
  return GetAdminAlertUseCase(ref.watch(adminAlertsRepositoryProvider));
});

final createAdminAlertUseCaseProvider = Provider<CreateAdminAlertUseCase>((
  ref,
) {
  return CreateAdminAlertUseCase(ref.watch(adminAlertsRepositoryProvider));
});
