import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/register_device_input.dart';
import '../admin_copy.dart';
import '../viewmodels/admin_devices_view_model.dart';
import '../widgets/admin_widgets.dart';

class RegisterDevicePage extends ConsumerStatefulWidget {
  const RegisterDevicePage({super.key});

  @override
  ConsumerState<RegisterDevicePage> createState() => _RegisterDevicePageState();
}

class _RegisterDevicePageState extends ConsumerState<RegisterDevicePage> {
  final _uid = TextEditingController();
  final _name = TextEditingController();
  final _credential = TextEditingController();
  final _alarm = TextEditingController();
  final _buffer = TextEditingController();
  final _ttl = TextEditingController();
  String _authType = DeviceCredentialTypes.passwordHash;

  @override
  void dispose() {
    _uid.dispose();
    _name.dispose();
    _credential.dispose();
    _alarm.dispose();
    _buffer.dispose();
    _ttl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(registerDeviceViewModelProvider);
    ref.listen(registerDeviceViewModelProvider, (prev, next) {
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
      if (next.succeeded && prev?.succeeded != true && next.createdId != null) {
        _credential.clear();
        context.go(AppRoutes.adminDevicePath(next.createdId!));
      }
    });

    String? fieldError(String key) => adminFieldError(
      key: key,
      field: state.field,
      message: state.error?.message,
    );

    return AdminFormScope(
      fields: [_uid, _name, _credential, _alarm, _buffer, _ttl],
      builder: (context, dirty) {
        return AdminLeaveGuard(
          dirty: dirty,
          child: AdminPageBody(
            title: AdminCopy.registerDevice,
            subtitle: AdminCopy.registerSubtitle,
            action: OutlinedButton(
              style: AdminButtonStyles.outline,
              onPressed: () => leaveAdminForm(
                context,
                dirty: dirty,
                location: AppRoutes.adminDevices,
              ),
              child: const Text('← ${AdminCopy.back}'),
            ),
            child: AdminFormFrame(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const AdminNotice(text: AdminCopy.d03Notice),
                  AdminPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        if (state.error != null && state.field == null)
                          AdminNotice(text: state.error!.message, danger: true),
                        AdminField(
                          label: AdminCopy.displayName,
                          controller: _name,
                          errorText: fieldError('displayName'),
                        ),
                        AdminField(
                          label: AdminCopy.hardwareUid,
                          controller: _uid,
                          errorText: fieldError('hardwareUid'),
                        ),
                        Text(
                          AdminCopy.sensor,
                          style: Theme.of(context).textTheme.labelMedium
                              ?.copyWith(
                                color: DesignTokens.muted,
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const SizedBox(height: 6),
                        const InputDecorator(
                          decoration: InputDecoration(),
                          child: Text(AdminCopy.sensorValue),
                        ),
                        const SizedBox(height: 13),
                        Text(
                          AdminCopy.authType,
                          style: Theme.of(context).textTheme.labelMedium
                              ?.copyWith(
                                color: DesignTokens.muted,
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const SizedBox(height: 6),
                        DropdownButtonFormField<String>(
                          initialValue: _authType,
                          decoration: InputDecoration(
                            errorText: fieldError('authType'),
                          ),
                          items: const [
                            DropdownMenuItem(
                              value: DeviceCredentialTypes.passwordHash,
                              child: Text(DeviceCredentialTypes.passwordHash),
                            ),
                            DropdownMenuItem(
                              value: DeviceCredentialTypes.certificate,
                              child: Text(DeviceCredentialTypes.certificate),
                            ),
                            DropdownMenuItem(
                              value: DeviceCredentialTypes.secretRef,
                              child: Text(DeviceCredentialTypes.secretRef),
                            ),
                          ],
                          onChanged: state.busy
                              ? null
                              : (value) {
                                  if (value == null) return;
                                  setState(() {
                                    _authType = value;
                                    _credential.clear();
                                  });
                                },
                        ),
                        const SizedBox(height: 13),
                        AdminField(
                          label: AdminCopy.credential,
                          controller: _credential,
                          obscure: true,
                          errorText: fieldError('credential'),
                        ),
                        AdminField(
                          label: AdminCopy.alarmRule,
                          controller: _alarm,
                          maxLines: 4,
                          errorText: fieldError('alarmRule'),
                        ),
                        AdminField(
                          label: AdminCopy.buffer,
                          controller: _buffer,
                          keyboardType: TextInputType.number,
                          errorText: fieldError('eventBufferCapacity'),
                        ),
                        AdminField(
                          label: AdminCopy.ttl,
                          controller: _ttl,
                          keyboardType: TextInputType.number,
                          errorText: fieldError('commandTtlSeconds'),
                        ),
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: state.busy ? null : _submit,
                          child: Text(
                            state.busy ? AdminCopy.busy : AdminCopy.save,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  void _submit() {
    ref
        .read(registerDeviceViewModelProvider.notifier)
        .submit(
          RegisterDeviceInput(
            hardwareUid: _uid.text,
            displayName: _name.text,
            authType: _authType,
            credentialValue: _credential.text,
            alarmRuleJson: _alarm.text,
            eventBufferCapacity: int.tryParse(_buffer.text.trim()) ?? 0,
            commandTtlSeconds: int.tryParse(_ttl.text.trim()) ?? 0,
          ),
        );
  }
}
