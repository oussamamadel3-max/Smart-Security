import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/entities/owner_account_entity.dart';
import '../admin_copy.dart';
import '../viewmodels/ownership_form_view_models.dart';
import '../widgets/admin_widgets.dart';

class LinkDevicePage extends ConsumerStatefulWidget {
  const LinkDevicePage({super.key, required this.deviceId});

  final String deviceId;

  @override
  ConsumerState<LinkDevicePage> createState() => _LinkDevicePageState();
}

class _LinkDevicePageState extends ConsumerState<LinkDevicePage> {
  String? _ownerId;

  @override
  Widget build(BuildContext context) {
    final provider = linkDeviceViewModelProvider(widget.deviceId);
    final state = ref.watch(provider);
    final owners = state.owners.where((o) => o.isActive && !o.isArchived);
    ref.listen(provider, (prev, next) {
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    return AdminPageBody(
      title: AdminCopy.link,
      subtitle: AdminCopy.linkSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminDevicePath(widget.deviceId)),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: state.loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AdminNotice(text: AdminCopy.pendingBanner),
                AdminFormFrame(
                  child: AdminPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _OwnerPicker(
                          owners: owners.toList(),
                          value: _ownerId,
                          onChanged: (value) =>
                              setState(() => _ownerId = value),
                        ),
                        if (state.result != null)
                          AdminNotice(
                            text:
                                '${state.result!.ownershipStatus.code} · canControl=${state.result!.canControl}. ${state.result!.ownershipSyncQueued ? AdminCopy.syncQueued : ''}',
                            tone: AdminNoticeTone.info,
                          ),
                        if (state.error != null)
                          AdminNotice(text: state.error!.message, danger: true),
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: state.busy || _ownerId == null
                              ? null
                              : () => ref
                                    .read(provider.notifier)
                                    .submit(_ownerId!),
                          child: Text(
                            state.busy ? AdminCopy.busy : AdminCopy.link,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 18),
                const AdminFormFrame(child: AdminProcessSteps()),
              ],
            ),
    );
  }
}

class TransferDevicePage extends ConsumerStatefulWidget {
  const TransferDevicePage({super.key, required this.deviceId});

  final String deviceId;

  @override
  ConsumerState<TransferDevicePage> createState() => _TransferDevicePageState();
}

class _TransferDevicePageState extends ConsumerState<TransferDevicePage> {
  String? _ownerId;

  @override
  Widget build(BuildContext context) {
    final provider = transferDeviceViewModelProvider(widget.deviceId);
    final state = ref.watch(provider);
    final owners = state.owners.where(
      (o) => o.isActive && !o.isArchived && o.id != state.excludedOwnerId,
    );
    ref.listen(provider, (prev, next) {
      final error = next.error;
      if (error != null &&
          error != prev?.error &&
          error.displayType != ErrorDisplayType.inline) {
        showErrorSnackBar(context, error);
      }
    });

    final result = state.result;
    return AdminPageBody(
      title: AdminCopy.transfer,
      subtitle: AdminCopy.transferSubtitle,
      action: OutlinedButton(
        style: AdminButtonStyles.outline,
        onPressed: () => context.go(AppRoutes.adminDevicePath(widget.deviceId)),
        child: const Text('← ${AdminCopy.back}'),
      ),
      child: state.loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AdminNotice(text: AdminCopy.activeAwaitingBanner),
                AdminFormFrame(
                  child: AdminPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _OwnerPicker(
                          label: AdminCopy.newOwner,
                          owners: owners.toList(),
                          value: _ownerId,
                          onChanged: (value) =>
                              setState(() => _ownerId = value),
                        ),
                        if (result != null)
                          AdminNotice(
                            text:
                                '${result.ownershipStatus.code} · awaiting=${result.awaitingOwnershipAck} · canPublish=${result.canPublishCommands}. ${result.ownershipSyncQueued ? AdminCopy.syncQueued : ''}',
                            tone: AdminNoticeTone.info,
                          ),
                        if (state.error != null)
                          AdminNotice(text: state.error!.message, danger: true),
                        FilledButton(
                          style: AdminButtonStyles.primary,
                          onPressed: state.busy || _ownerId == null
                              ? null
                              : () => ref
                                    .read(provider.notifier)
                                    .submit(_ownerId!),
                          child: Text(
                            state.busy ? AdminCopy.busy : AdminCopy.transfer,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 18),
                const AdminFormFrame(child: AdminProcessSteps()),
              ],
            ),
    );
  }
}

class _OwnerPicker extends StatelessWidget {
  const _OwnerPicker({
    required this.owners,
    required this.value,
    required this.onChanged,
    this.label = AdminCopy.owner,
  });

  final List<OwnerAccountEntity> owners;
  final String? value;
  final ValueChanged<String?> onChanged;
  final String label;

  @override
  Widget build(BuildContext context) {
    if (owners.isEmpty) {
      return const Padding(
        padding: EdgeInsets.only(bottom: 13),
        child: AdminNotice(text: AdminCopy.noActiveOwners, danger: true),
      );
    }
    return Padding(
      padding: const EdgeInsets.only(bottom: 13),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: DesignTokens.muted,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 6),
          DropdownButtonFormField<String>(
            initialValue: value,
            decoration: const InputDecoration(),
            items: [
              for (final owner in owners)
                DropdownMenuItem(
                  value: owner.id,
                  child: Text('${owner.fullName} · ${owner.userName ?? ''}'),
                ),
            ],
            onChanged: onChanged,
          ),
        ],
      ),
    );
  }
}
