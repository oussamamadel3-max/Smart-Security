import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/notifications/data/models/owner_notification_dto.dart';
import 'package:security_mobile/features/notifications/domain/repositories/owner_notifications_repository.dart';
import 'package:security_mobile/features/notifications/domain/usecases/open_notification_usecase.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

// OwnerNotificationListItemDto used by listMine stub.

// OwnerNotificationListItemDto used by listMine stub.

class _FakeNotificationsRepo implements OwnerNotificationsRepository {
  _FakeNotificationsRepo(this._result);

  final Result<OwnerNotificationDetailDto> _result;

  @override
  Future<Result<List<OwnerNotificationListItemDto>>> listMine() async {
    return const Success([]);
  }

  @override
  Future<Result<OwnerNotificationDetailDto>> getById(String notificationId) async {
    return _result;
  }
}

void main() {
  test('navigates using API detail after ownership check', () async {
    final useCase = OpenNotificationUseCase(
      _FakeNotificationsRepo(
        const Success(
          OwnerNotificationDetailDto(
            notificationId: 'n1',
            notificationType: 'ALARM',
            status: 'SENT',
            createdAt: '2026-11-17T10:00:00Z',
            deviceId: 'd1',
            alarmEventId: 'a1',
            alarmEventDeviceId: 'd1',
          ),
        ),
      ),
    );

    final result = await useCase({'notificationId': 'n1'});
    expect(result, isA<OpenNotificationNavigateAlarm>());
    final nav = result as OpenNotificationNavigateAlarm;
    expect(nav.deviceId, 'd1');
    expect(nav.alarmEventId, 'a1');
  });

  test('403/404 becomes denied without inventing local details', () async {
    final useCase = OpenNotificationUseCase(
      _FakeNotificationsRepo(
        const Failure_(
          Failure.forbidden(message: 'لا صلاحية'),
        ),
      ),
    );

    final result = await useCase({'notificationId': 'old'});
    expect(result, isA<OpenNotificationDenied>());
  });

  test('missing notificationId is unsupported', () async {
    final useCase = OpenNotificationUseCase(
      _FakeNotificationsRepo(
        const Failure_(Failure.notFound(message: 'x')),
      ),
    );

    final result = await useCase({'deviceId': 'd1'});
    expect(result, isA<OpenNotificationUnsupported>());
  });
}
