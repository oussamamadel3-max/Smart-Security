import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../admin_copy.dart';
import '../viewmodels/owner_account_form_view_models.dart';
import '../widgets/admin_widgets.dart';

class CreateOwnerPage extends ConsumerStatefulWidget {
  const CreateOwnerPage({super.key});

  @override
  ConsumerState<CreateOwnerPage> createState() => _CreateOwnerPageState();
}

class _CreateOwnerPageState extends ConsumerState<CreateOwnerPage> {
  final _userName = TextEditingController();
  final _fullName = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _userName.dispose();
    _fullName.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(createOwnerViewModelProvider);
    ref.listen(createOwnerViewModelProvider, (prev, next) {
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
      if (next.succeeded && prev?.succeeded != true && next.createdId != null) {
        _password.clear();
        context.go(AppRoutes.adminAccountPath(next.createdId!));
      }
    });

    return AdminFormScope(
      fields: [_userName, _fullName, _email, _password],
      builder: (context, dirty) {
        return AdminLeaveGuard(
          dirty: dirty,
          child: AdminPageBody(
            title: AdminCopy.addAccount,
            subtitle: AdminCopy.permissionNote,
            action: OutlinedButton(
              style: AdminButtonStyles.outline,
              onPressed: () => leaveAdminForm(
                context,
                dirty: dirty,
                location: AppRoutes.adminAccounts,
              ),
              child: const Text('← ${AdminCopy.back}'),
            ),
            child: AdminFormFrame(
              maxWidth: 720,
              child: AdminPanel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(AdminCopy.roleOwner),
                    const SizedBox(height: 13),
                    if (state.error != null && state.field == null)
                      AdminNotice(text: state.error!.message, danger: true),
                    AdminField(
                      label: AdminCopy.userName,
                      controller: _userName,
                      errorText: adminFieldError(
                        key: 'userName',
                        field: state.field,
                        message: state.error?.message,
                      ),
                    ),
                    AdminField(
                      label: AdminCopy.fullName,
                      controller: _fullName,
                      errorText: adminFieldError(
                        key: 'fullName',
                        field: state.field,
                        message: state.error?.message,
                      ),
                    ),
                    AdminField(
                      label: AdminCopy.email,
                      controller: _email,
                      errorText: adminFieldError(
                        key: 'email',
                        field: state.field,
                        message: state.error?.message,
                      ),
                    ),
                    AdminField(
                      label: AdminCopy.password,
                      controller: _password,
                      obscure: true,
                      errorText: adminFieldError(
                        key: 'password',
                        field: state.field,
                        message: state.error?.message,
                      ),
                    ),
                    AdminCommandRow(
                      children: [
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: state.busy
                              ? null
                              : () => ref
                                    .read(createOwnerViewModelProvider.notifier)
                                    .submit(
                                      userName: _userName.text,
                                      fullName: _fullName.text,
                                      password: _password.text,
                                      email: _email.text,
                                    ),
                          child: Text(
                            state.busy ? AdminCopy.busy : AdminCopy.save,
                          ),
                        ),
                        OutlinedButton(
                          style: AdminButtonStyles.outline,
                          onPressed: state.busy
                              ? null
                              : () => leaveAdminForm(
                                  context,
                                  dirty: dirty,
                                  location: AppRoutes.adminAccounts,
                                ),
                          child: const Text(AdminCopy.cancel),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}
