import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/config/app_environment.dart';
import '../../../../core/session/session_bootstrap.dart';

class AdminHomePlaceholderPage extends ConsumerWidget {
  const AdminHomePlaceholderPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final env = AppEnvironment.current;
    return Scaffold(
      appBar: AppBar(
        title: Text('${tr.appName} · أدمن'),
        backgroundColor: DesignTokens.navy,
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: () =>
                ref.read(sessionBootstrapProvider.notifier).clear(),
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: MyEmptyState(
        type: MyEmptyType.empty,
        title: tr.bootstrapPlaceholder,
        subtitle: 'Admin shell · ${env.label} · ${env.apiBaseUrl}',
      ),
    );
  }
}
