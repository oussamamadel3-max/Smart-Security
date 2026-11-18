import 'package:admin_web/features/admin/domain/entities/admin_device_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_device_indicators_entity.dart';
import 'package:admin_web/features/admin/domain/entities/admin_mutation_entities.dart';
import 'package:admin_web/features/admin/domain/register_device_input.dart';
import 'package:admin_web/features/admin/domain/repositories/admin_devices_repository.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_device_detail_page.dart';
import 'package:admin_web/features/admin/presentation/providers/admin_providers.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _Devices implements AdminDevicesRepository {
  @override
  Future<Result<ArchivedDeviceEntity>> archive(String deviceId) async {
    return const Failure_(Failure.forbidden());
  }

  @override
  void clearCache() {}

  @override
  Future<Result<AdminDeviceEntity>> getById(String deviceId) async {
    return Success(
      AdminDeviceEntity(
        deviceId: deviceId,
        hardwareUid: 'HW-1',
        displayName: 'المنزل',
        lifecycleStatus: 'REGISTERED',
        currentOwnershipVersion: 1,
        confirmedOwnershipVersion: 0,
        awaitingOwnershipAck: false,
        ownershipStatus: OwnershipStatus.pending,
        indicators: const AdminDeviceIndicatorsEntity(
          connection: ConnectionStatus.offline,
          arm: ArmStatus.unknown,
          siren: SirenStatus.unknown,
          health: DiagnosticStatus.unknown,
          clockTrusted: false,
        ),
      ),
    );
  }

  @override
  Future<Result<LinkedDeviceEntity>> link({
    required String deviceId,
    required String ownerUserId,
  }) async {
    return const Failure_(Failure.conflict());
  }

  @override
  Future<Result<List<AdminDeviceEntity>>> list() async => const Success([]);

  @override
  Future<Result<RegisteredDeviceEntity>> register(
    RegisterDeviceInput input,
  ) async {
    return const Failure_(Failure.conflict());
  }

  @override
  Future<Result<TransferredDeviceEntity>> transfer({
    required String deviceId,
    required String newOwnerUserId,
  }) async {
    return const Failure_(Failure.conflict());
  }

  @override
  Future<Result<UnlinkedDeviceEntity>> unlink(String deviceId) async {
    return const Failure_(Failure.forbidden());
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = false;

  testWidgets('admin device screen shows PENDING and no owner commands', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          adminDevicesRepositoryProvider.overrideWithValue(_Devices()),
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
          home: AdminDeviceDetailPage(deviceId: 'device-1'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.textContaining('PENDING'), findsWidgets);
    expect(find.text('تفعيل الحماية'), findsNothing);
    expect(find.text('تعطيل الحماية'), findsNothing);
    expect(find.text('كتم الصفارة'), findsNothing);
    expect(find.text('اختبار الصفارة'), findsNothing);
    expect(find.text('إيقاف الاختبار'), findsNothing);
  });
}
