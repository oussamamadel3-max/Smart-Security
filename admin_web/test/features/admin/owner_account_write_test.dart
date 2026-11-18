import 'package:admin_web/features/admin/domain/create_owner_input.dart';
import 'package:admin_web/features/admin/domain/entities/owner_account_entity.dart';
import 'package:admin_web/features/admin/domain/repositories/owner_accounts_repository.dart';
import 'package:admin_web/features/admin/presentation/admin_write_state.dart';
import 'package:admin_web/features/admin/presentation/providers/admin_providers.dart';
import 'package:admin_web/features/admin/presentation/viewmodels/owner_account_form_view_models.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _Owners implements OwnerAccountsRepository {
  _Owners(this.failure);

  final Failure failure;
  int writes = 0;

  final account = OwnerAccountEntity(
    id: 'owner-1',
    fullName: 'أحمد',
    userName: 'ahmad',
    isActive: true,
    createdAt: DateTime.utc(2026, 1, 1),
  );

  @override
  Future<Result<bool>> archive(String ownerId) async {
    writes += 1;
    return Failure_(failure);
  }

  @override
  void clearCache() {}

  @override
  Future<Result<OwnerAccountEntity>> create(CreateOwnerInput input) async {
    writes += 1;
    return Failure_(failure);
  }

  @override
  Future<Result<bool>> disable(String ownerId) async {
    writes += 1;
    return Failure_(failure);
  }

  @override
  Future<Result<OwnerAccountEntity>> getById(String ownerId) async {
    return Success(account);
  }

  @override
  Future<Result<List<OwnerAccountEntity>>> list() async => Success([account]);

  @override
  Future<Result<OwnerAccountEntity>> update(
    String ownerId,
    UpdateOwnerInput input,
  ) async {
    writes += 1;
    return Failure_(failure);
  }
}

void main() {
  test('401 403 and 409 do not mark owner writes successful', () async {
    final cases = [
      const Failure_(
        Failure.unauthorized(message: 'انتهت جلستك، يرجى تسجيل الدخول'),
      ),
      const Failure_(
        Failure.forbidden(message: 'ليس لديك صلاحية للقيام بهذا الإجراء'),
      ),
      const Failure_(
        Failure.conflict(message: 'لا يمكن أرشفة حساب مرتبط بأجهزة'),
      ),
    ];

    for (final failure in cases) {
      final repo = _Owners(failure.failureOrNull!);
      final container = ProviderContainer(
        overrides: [
          ownerAccountsRepositoryProvider.overrideWithValue(repo),
        ],
      );
      addTearDown(container.dispose);
      final subscription = container.listen(
        createOwnerViewModelProvider,
        (_, _) {},
      );
      addTearDown(subscription.close);
      final notifier = container.read(createOwnerViewModelProvider.notifier);
      await notifier.submit(
        userName: 'owner1',
        fullName: 'أحمد محمود',
        password: 'long-enough',
      );
      final state = container.read(createOwnerViewModelProvider);
      expect(adminWriteAccepted(failure), isFalse);
      expect(state.succeeded, isFalse);
      expect(state.createdId, isNull);
      expect(state.error?.message, failure.failureOrNull!.message);
      expect(repo.writes, 1);
    }
  });

  test('archive conflict keeps the account and does not claim success', () async {
    const conflict = Failure_(
      Failure.conflict(message: 'لا يمكن أرشفة حساب مرتبط بأجهزة'),
    );
    final repo = _Owners(conflict.failureOrNull!);
    final container = ProviderContainer(
      overrides: [ownerAccountsRepositoryProvider.overrideWithValue(repo)],
    );
    addTearDown(container.dispose);
    final provider = ownerAccountDetailViewModelProvider('owner-1');
    container.listen(provider, (_, _) {});
    await Future<void>.delayed(Duration.zero);
    await container.read(provider.notifier).archive();
    final state = container.read(provider);
    expect(state.write.succeeded, isFalse);
    expect(state.account?.isArchived, isFalse);
    expect(state.account?.isActive, isTrue);
    expect(state.write.error?.message, contains('أرشفة'));
  });
}
