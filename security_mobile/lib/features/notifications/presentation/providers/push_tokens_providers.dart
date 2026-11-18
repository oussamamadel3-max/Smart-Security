import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/mobile_network_providers.dart';
import '../../../../core/push/push_token_id_store.dart';
import '../../data/datasources/owner_notifications_remote_datasource.dart';
import '../../data/datasources/push_tokens_remote_datasource.dart';
import '../../data/repositories/owner_notifications_repository_impl.dart';
import '../../data/repositories/push_tokens_repository_impl.dart';
import '../../domain/repositories/owner_notifications_repository.dart';
import '../../domain/repositories/push_tokens_repository.dart';
import '../../domain/usecases/open_notification_usecase.dart';

final pushTokenIdStoreProvider = Provider<PushTokenIdStore>((ref) {
  return PushTokenIdStore(ref.watch(flutterSecureStorageProvider));
});

final pushTokensRepositoryProvider = Provider<PushTokensRepository>((ref) {
  return PushTokensRepositoryImpl(
    PushTokensRemoteDataSource(ref.watch(mobileDioProvider)),
    ref.watch(pushTokenIdStoreProvider),
  );
});

final ownerNotificationsRepositoryProvider =
    Provider<OwnerNotificationsRepository>((ref) {
  return OwnerNotificationsRepositoryImpl(
    OwnerNotificationsRemoteDataSource(ref.watch(mobileDioProvider)),
  );
});

final openNotificationUseCaseProvider =
    Provider<OpenNotificationUseCase>((ref) {
  return OpenNotificationUseCase(
    ref.watch(ownerNotificationsRepositoryProvider),
  );
});
