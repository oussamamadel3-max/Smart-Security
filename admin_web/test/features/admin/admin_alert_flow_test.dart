import 'package:admin_web/features/admin/domain/admin_alert_query.dart';
import 'package:admin_web/features/admin/domain/audit_log_query.dart';
import 'package:admin_web/features/admin/domain/create_admin_alert_input.dart';
import 'package:admin_web/features/admin/domain/entities/admin_alert_entity.dart';
import 'package:admin_web/features/admin/domain/entities/audit_log_entity.dart';
import 'package:admin_web/features/admin/domain/entities/paged_page.dart';
import 'package:admin_web/features/admin/domain/repositories/admin_alerts_repository.dart';
import 'package:admin_web/features/admin/domain/repositories/audit_logs_repository.dart';
import 'package:admin_web/features/admin/presentation/admin_alert_facts.dart';
import 'package:admin_web/features/admin/presentation/admin_copy.dart';
import 'package:admin_web/features/admin/presentation/pages/admin_alerts_page.dart';
import 'package:admin_web/features/admin/presentation/providers/admin_providers.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/admin_alerts_view_model.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/audit_logs_view_model.dart';
import 'package:admin_web/features/admin/presentation/widgets/admin_alert_outcome_panel.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

const _recipient = '33333333-3333-3333-3333-333333333333';

AdminAlertEntity _alert({
  required String status,
  required bool attempted,
  DateTime? lastAttemptAt,
  String title = 'تنبيه محفوظ',
  String body = 'النص يبقى ظاهراً',
}) {
  return AdminAlertEntity(
    adminAlertId: '11111111-1111-1111-1111-111111111111',
    senderUserId: '22222222-2222-2222-2222-222222222222',
    recipientUserId: _recipient,
    title: title,
    body: body,
    createdAt: DateTime.utc(2026, 11, 18, 1),
    deliveryAttemptStatus: status,
    deliveryAttempted: attempted,
    lastAttemptAt: lastAttemptAt,
    notificationStatus: status,
  );
}

class _Alerts implements AdminAlertsRepository {
  _Alerts(this.onCreate);

  final Result<AdminAlertEntity> onCreate;
  int creates = 0;

  @override
  Future<Result<AdminAlertEntity>> create(CreateAdminAlertInput input) async {
    creates += 1;
    return onCreate;
  }

  @override
  Future<Result<AdminAlertEntity>> getById(String alertId) async {
    return const Failure_(Failure.forbidden());
  }

  @override
  Future<Result<PagedPage<AdminAlertEntity>>> list(
    AdminAlertQuery query,
  ) async {
    return const Success(
      PagedPage(items: [], page: 1, pageSize: 20, totalCount: 0),
    );
  }
}

class _Audit implements AuditLogsRepository {
  var forbid = false;

  @override
  Future<Result<PagedPage<AuditLogEntity>>> query(AuditLogQuery query) async {
    if (forbid) return const Failure_(Failure.forbidden());
    return Success(
      PagedPage(
        items: [
          AuditLogEntity(
            id: '112',
            action: 'OWNERSHIP_TRANSFERRED',
            targetType: 'OWNERSHIP',
            result: 'SUCCESS',
            occurredAt: DateTime.utc(2026, 11, 18, 8, 20),
            targetId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            deviceId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          ),
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
      ),
    );
  }
}

Future<void> _untilSettled(bool Function() isLoading) async {
  for (var i = 0; i < 8 && isLoading(); i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

void main() {
  test('403 does not mark alert create as saved or read', () async {
    final repo = _Alerts(const Failure_(Failure.forbidden()));
    final container = ProviderContainer(
      overrides: [adminAlertsRepositoryProvider.overrideWithValue(repo)],
    );
    addTearDown(container.dispose);
    final sub = container.listen(createAdminAlertViewModelProvider, (_, _) {});
    addTearDown(sub.close);

    await container
        .read(createAdminAlertViewModelProvider.notifier)
        .submit(
          recipientUserId: _recipient,
          title: 'عنوان',
          body: 'نص التنبيه',
        );

    final state = container.read(createAdminAlertViewModelProvider);
    expect(repo.creates, 1);
    expect(state.succeeded, isFalse);
    expect(state.result, isNull);
    expect(state.error?.message, 'ليس لديك صلاحية للقيام بهذا الإجراء');
  });

  test('403 on audit query clears rows instead of keeping them', () async {
    final repo = _Audit();
    final container = ProviderContainer(
      overrides: [auditLogsRepositoryProvider.overrideWithValue(repo)],
    );
    addTearDown(container.dispose);
    final sub = container.listen(auditLogsViewModelProvider, (_, _) {});
    addTearDown(sub.close);

    await container.read(auditLogsViewModelProvider.notifier).load();
    await _untilSettled(
      () => container.read(auditLogsViewModelProvider).loading,
    );
    expect(container.read(auditLogsViewModelProvider).items, isNotEmpty);

    repo.forbid = true;
    await container.read(auditLogsViewModelProvider.notifier).load();
    await _untilSettled(
      () => container.read(auditLogsViewModelProvider).loading,
    );
    final state = container.read(auditLogsViewModelProvider);
    expect(state.items, isEmpty);
    expect(state.error?.message, contains('صلاحية'));
  });

  test('403 on alert detail does not keep a readable alert', () async {
    final repo = _Alerts(Success(_alert(status: 'PENDING', attempted: false)));
    final container = ProviderContainer(
      overrides: [adminAlertsRepositoryProvider.overrideWithValue(repo)],
    );
    addTearDown(container.dispose);
    const id = '11111111-1111-1111-1111-111111111111';
    final provider = adminAlertDetailViewModelProvider(id);
    final sub = container.listen(provider, (_, _) {});
    addTearDown(sub.close);

    await container.read(provider.notifier).load();
    final state = container.read(provider);
    expect(state.alert, isNull);
    expect(state.error?.message, contains('صلاحية'));
  });

  testWidgets('create result shows server attempt facts and no read claim', (
    tester,
  ) async {
    final repo = _Alerts(
      Success(
        _alert(status: 'PENDING', attempted: false, title: 'تنبيه محفوظ'),
      ),
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [adminAlertsRepositoryProvider.overrideWithValue(repo)],
        child: const MaterialApp(home: Scaffold(body: CreateAdminAlertPage())),
      ),
    );

    await tester.enterText(find.byType(TextField).at(0), _recipient);
    await tester.enterText(find.byType(TextField).at(1), 'تنبيه محفوظ');
    await tester.enterText(find.byType(TextField).at(2), 'النص يبقى ظاهراً');
    await tester.tap(find.text(AdminCopy.save));
    await tester.pumpAndSettle();

    expect(repo.creates, 1);
    expect(find.text(AdminAlertFacts.requestAccepted), findsOneWidget);
    expect(find.text(AdminAlertFacts.noAttempt), findsOneWidget);
    expect(find.text(AdminAlertFacts.deliveryPending), findsOneWidget);
    expect(find.text(AdminAlertFacts.noReadEvidence), findsOneWidget);
    expect(find.text('مقروء'), findsNothing);
    expect(find.text(AdminAlertFacts.deliveryAccepted), findsNothing);
  });

  testWidgets('failed delivery remains on screen without a read claim', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: AdminAlertOutcomePanel(
            alert: _alert(
              status: 'FAILED',
              attempted: true,
              lastAttemptAt: DateTime.utc(2026, 11, 18, 1, 5),
              title: 'فشل ظاهر',
              body: 'النص الأصلي',
            ),
          ),
        ),
      ),
    );

    expect(find.text('فشل ظاهر'), findsOneWidget);
    expect(find.text('النص الأصلي'), findsOneWidget);
    expect(find.text(AdminAlertFacts.deliveryFailed), findsWidgets);
    expect(find.text(AdminAlertFacts.noReadEvidence), findsOneWidget);
    expect(find.text('مقروء'), findsNothing);
    expect(find.text(AdminAlertFacts.deliveryAccepted), findsNothing);
  });
}
