import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../admin_copy.dart';
import '../viewmodels/owner_account_form_view_models.dart';
import '../widgets/admin_widgets.dart';

class OwnerAccountDetailPage extends ConsumerStatefulWidget {
  const OwnerAccountDetailPage({super.key, required this.ownerId});

  final String ownerId;

  @override
  ConsumerState<OwnerAccountDetailPage> createState() =>
      _OwnerAccountDetailPageState();
}

class _OwnerAccountDetailPageState
    extends ConsumerState<OwnerAccountDetailPage> {
  final _fullName = TextEditingController();
  final _email = TextEditingController();
  var _editing = false;
  var _seeded = false;

  @override
  void dispose() {
    _fullName.dispose();
    _email.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = ownerAccountDetailViewModelProvider(widget.ownerId);
    final state = ref.watch(provider);
    final account = state.account;
    if (account != null && !_seeded) {
      _seeded = true;
      _fullName.text = account.fullName;
      _email.text = account.email ?? '';
    }

    ref.listen(provider, (prev, next) {
      final error = next.write.error;
      if (error != null &&
          error != prev?.write.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.accountDetail,
      subtitle: AdminCopy.permissionNote,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminAccounts),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: state.loading && account == null
          ? const Center(child: CircularProgressIndicator())
          : account == null
          ? MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.write.error?.message ?? '',
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: () => ref.read(provider.notifier).load(),
            )
          : AdminPanel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AdminSectionTitle(
                    title: account.fullName,
                    action: adminAccountStatusPill(account.statusCode),
                  ),
                  AdminKvGrid(
                    entries: [
                      (AdminCopy.email, account.email ?? AdminCopy.noEmail),
                      (AdminCopy.userName, account.userName ?? '—'),
                      (AdminCopy.roleOwner, AdminCopy.roleOwnerPill),
                      (
                        AdminCopy.status,
                        AdminCopy.accountStatus(account.statusCode),
                      ),
                      (AdminCopy.createdAt, formatAdminTime(account.createdAt)),
                    ],
                  ),
                  if (_editing) ...[
                    const SizedBox(height: 17),
                    if (state.write.error != null && state.write.field == null)
                      AdminNotice(
                        text: state.write.error!.message,
                        danger: true,
                      ),
                    AdminField(
                      label: AdminCopy.fullName,
                      controller: _fullName,
                      errorText: adminFieldError(
                        key: 'fullName',
                        field: state.write.field,
                        message: state.write.error?.message,
                      ),
                    ),
                    AdminField(
                      label: AdminCopy.email,
                      controller: _email,
                      errorText: adminFieldError(
                        key: 'email',
                        field: state.write.field,
                        message: state.write.error?.message,
                      ),
                    ),
                    FilledButton(
                      style: AdminButtonStyles.primary,
                      onPressed: state.write.busy
                          ? null
                          : () => ref
                                .read(provider.notifier)
                                .save(
                                  fullName: _fullName.text,
                                  email: _email.text,
                                ),
                      child: Text(
                        state.write.busy ? AdminCopy.busy : AdminCopy.save,
                      ),
                    ),
                  ],
                  AdminCommandRow(
                    children: [
                      if (!_editing)
                        OutlinedButton(
                          style: AdminButtonStyles.outline,
                          onPressed: account.isArchived
                              ? null
                              : () => setState(() => _editing = true),
                          child: const Text(AdminCopy.edit),
                        ),
                      FilledButton(
                        style: AdminButtonStyles.danger,
                        onPressed: account.isActive && !state.write.busy
                            ? () => _confirm(
                                AdminCopy.disable,
                                AdminCopy.disableConfirm,
                                () => ref.read(provider.notifier).disable(),
                              )
                            : null,
                        child: const Text(AdminCopy.disable),
                      ),
                      FilledButton(
                        style: AdminButtonStyles.danger,
                        onPressed: account.isArchived || state.write.busy
                            ? null
                            : () => _confirm(
                                AdminCopy.archiveAccount,
                                AdminCopy.archiveConfirm,
                                () => ref.read(provider.notifier).archive(),
                              ),
                        child: const Text(AdminCopy.archiveAccount),
                      ),
                    ],
                  ),
                ],
              ),
            ),
    );
  }

  Future<void> _confirm(
    String title,
    String message,
    Future<void> Function() action,
  ) async {
    final ok = await showAdminConfirmDialog(
      context: context,
      title: title,
      message: message,
      danger: true,
    );
    if (ok == true) await action();
  }
}
