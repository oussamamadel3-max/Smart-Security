import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../devices/presentation/widgets/kv_row.dart';

class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final theme = Theme.of(context);
    final user = ref.watch(authSessionProvider.select((s) => s.user));
    final env = AppEnvironment.current;

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(title: Text(tr.navProfile)),
      body: ListView(
        padding: const EdgeInsets.all(DesignTokens.spaceLg),
        children: [
          Container(
            padding: const EdgeInsets.all(DesignTokens.spaceXl),
            decoration: BoxDecoration(
              color: DesignTokens.card,
              borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
              border: Border.all(color: DesignTokens.border),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr.accountSectionTitle, style: theme.textTheme.titleMedium),
                const SizedBox(height: DesignTokens.spaceLg),
                KvRow(tr.username, user?.userName ?? '—', labelWidth: 120),
                KvRow(tr.profileDisplayName, user?.fullName ?? '—',
                    labelWidth: 120),
                KvRow(tr.email, user?.email ?? '—', labelWidth: 120),
                KvRow(
                  tr.profileRoleLabel,
                  user?.isOwner == true
                      ? tr.ownerRoleLabel
                      : (user?.roles.join(', ') ?? '—'),
                  labelWidth: 120,
                ),
                KvRow(tr.profileEnvironment, env.label, labelWidth: 120),
              ],
            ),
          ),
          const SizedBox(height: DesignTokens.spaceLg),
          ListTile(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
              side: const BorderSide(color: DesignTokens.border),
            ),
            tileColor: DesignTokens.card,
            leading: const Icon(Icons.language),
            title: Text(tr.language),
            onTap: () => LanguageDialog.show(
              context,
              ref,
              ref.read(localeProvider),
            ),
          ),
          const SizedBox(height: DesignTokens.spaceMd),
          Container(
            padding: const EdgeInsets.all(DesignTokens.spaceMd),
            decoration: BoxDecoration(
              color: DesignTokens.infoSoftBg,
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
            ),
            child: Text(
              tr.profileFcmNote,
              style: theme.textTheme.bodySmall?.copyWith(
                color: DesignTokens.infoSoftText,
              ),
            ),
          ),
          const SizedBox(height: DesignTokens.spaceXl),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: DesignTokens.danger,
              foregroundColor: DesignTokens.white,
            ),
            onPressed: () async {
              final confirmed = await MyDialog.show(
                context: context,
                type: MyDialogType.confirm,
                title: tr.logout,
                message: tr.logoutConfirm,
                confirmLabel: tr.logout,
                cancelLabel: tr.cancel,
              );
              if (confirmed == true) {
                await ref.read(authSessionProvider.notifier).logout();
              }
            },
            child: Text(tr.logout),
          ),
        ],
      ),
    );
  }
}
