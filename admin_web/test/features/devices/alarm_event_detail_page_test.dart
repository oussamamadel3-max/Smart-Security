import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/devices/domain/entities/alarm_event_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/repositories/device_alarm_events_repository.dart';
import 'package:admin_web/features/devices/domain/repositories/device_commands_repository.dart';
import 'package:admin_web/features/devices/presentation/pages/alarm_event_detail_page.dart';
import 'package:admin_web/features/devices/presentation/providers/devices_providers.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _OwnerSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(id: 'owner-1', roles: ['OWNER']),
    );
  }
}

class _EventsRepo implements DeviceAlarmEventsRepository {
  @override
  Future<Result<List<AlarmEventEntity>>> listByDevice(String deviceId) async {
    return const Success([]);
  }

  @override
  Future<Result<AlarmEventEntity>> getById({
    required String deviceId,
    required String alarmEventId,
  }) async {
    return const Failure_(Failure.notFound(message: 'الحادثة غير موجودة.'));
  }

  @override
  void clearCache() {}

  @override
  void dropDevice(String deviceId) {}
}

class _CommandsRepo implements DeviceCommandsRepository {
  @override
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) async {
    return const Failure_(Failure.network());
  }

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async {
    return const Failure_(Failure.notFound());
  }

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async {
    return const Success([]);
  }

  @override
  void clearCache() {}

  @override
  void dropDevice(String deviceId) {}
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets('foreign alarm id renders the error and not another event', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authSessionProvider.overrideWith(_OwnerSession.new),
          deviceAlarmEventsRepositoryProvider.overrideWithValue(_EventsRepo()),
          deviceCommandsRepositoryProvider.overrideWithValue(_CommandsRepo()),
        ],
        child: const MaterialApp(
          locale: Locale('ar'),
          supportedLocales: kSupportedLocales,
          localizationsDelegates: [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          home: AlarmEventDetailPage(
            deviceId: 'device-1',
            alarmEventId: 'foreign-event',
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('الحادثة غير موجودة.'), findsOneWidget);
    expect(find.text('secret-event-uid'), findsNothing);
    expect(find.text('foreign-event'), findsNothing);
  });
}
