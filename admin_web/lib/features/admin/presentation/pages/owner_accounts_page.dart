import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../admin_copy.dart';
import '../viewmodels/owner_accounts_view_model.dart';
import '../widgets/admin_widgets.dart';

class OwnerAccountsPage extends ConsumerStatefulWidget {
  const OwnerAccountsPage({super.key});

  @override
  ConsumerState<OwnerAccountsPage> createState() => _OwnerAccountsPageState();
}

class _OwnerAccountsPageState extends ConsumerState<OwnerAccountsPage> {
  String _query = '';
  String _filter = AdminCopy.allStatuses;

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(ownerAccountsViewModelProvider);
    final vm = ref.read(ownerAccountsViewModelProvider.notifier);
    ref.listen(ownerAccountsViewModelProvider, (prev, next) {
      final error = next.error;
      if (error == null || error == prev?.error) return;
      if (error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    final q = _query.trim().toLowerCase();
    final rows = state.accounts.where((account) {
      if (_filter == AdminCopy.filterActive && account.statusCode != 'ACTIVE') {
        return false;
      }
      if (_filter == AdminCopy.filterDisabled &&
          account.statusCode != 'DISABLED') {
        return false;
      }
      if (_filter == AdminCopy.filterArchived &&
          account.statusCode != 'ARCHIVED') {
        return false;
      }
      if (q.isEmpty) return true;
      return '${account.fullName} ${account.userName ?? ''} ${account.email ?? ''}'
          .toLowerCase()
          .contains(q);
    }).toList();

    return AdminPageBody(
      title: AdminCopy.accounts,
      subtitle: AdminCopy.accountsSubtitle,
      action: FilledButton(
        style: AdminButtonStyles.primary,
        onPressed: () => context.go(AppRoutes.adminAccountNew),
        child: const Text('⊕ ${AdminCopy.addAccount}'),
      ),
      child: Column(
        children: [
          const AdminNotice(text: AdminCopy.permissionNote),
          if (state.loading)
            const Padding(
              padding: EdgeInsets.all(32),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (state.error != null && state.accounts.isEmpty)
            MyEmptyState(
              type: MyEmptyType.error,
              title: context.tr.error,
              subtitle: state.error!.message,
              primaryActionLabel: context.tr.retry,
              onPrimaryAction: vm.load,
            )
          else if (state.accounts.isEmpty)
            const MyEmptyState(
              type: MyEmptyType.empty,
              title: AdminCopy.emptyAccounts,
              subtitle: AdminCopy.accountsSubtitle,
            )
          else
            AdminPanel(
              child: Column(
                children: [
                  AdminToolbar(
                    search: AdminSearchField(
                      hint: AdminCopy.searchHint,
                      onChanged: (value) => setState(() => _query = value),
                    ),
                    filter: AdminFilterMenu(
                      value: _filter,
                      options: const [
                        AdminCopy.allStatuses,
                        AdminCopy.filterActive,
                        AdminCopy.filterDisabled,
                        AdminCopy.filterArchived,
                      ],
                      onChanged: (value) => setState(() => _filter = value),
                    ),
                  ),
                  AdminTable(
                    headers: const [
                      AdminCopy.fullName,
                      AdminCopy.userName,
                      AdminCopy.email,
                      AdminCopy.roleOwnerPill,
                      AdminCopy.status,
                      '',
                    ],
                    rows: [
                      for (final account in rows)
                        [
                          Text(
                            account.fullName,
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          Text(account.userName ?? '—'),
                          Text(account.email ?? AdminCopy.noEmail),
                          const AdminPill(
                            AdminCopy.roleOwnerPill,
                            AdminPillTone.blue,
                          ),
                          adminAccountStatusPill(account.statusCode),
                          AdminTextLink(
                            label: AdminCopy.detailsLink,
                            onPressed: () => context.go(
                              AppRoutes.adminAccountPath(account.id),
                            ),
                          ),
                        ],
                    ],
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
