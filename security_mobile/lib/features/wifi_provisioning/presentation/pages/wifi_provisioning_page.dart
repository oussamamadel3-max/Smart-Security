import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/router/app_routes.dart';
import '../../domain/local_provisioning_client.dart';
import '../viewmodels/wifi_provisioning_view_model.dart';

/// Local Wi-Fi setup — isolated from cloud ARM/DISARM command paths.
class WifiProvisioningPage extends ConsumerWidget {
  const WifiProvisioningPage({super.key, required this.deviceId});

  final String deviceId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tr = context.tr;
    final state = ref.watch(wifiProvisioningViewModelProvider(deviceId));
    final vm = ref.read(wifiProvisioningViewModelProvider(deviceId).notifier);
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: DesignTokens.background,
      appBar: AppBar(
        title: Text(tr.wifiSetupTitle),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.deviceDetailPath(deviceId)),
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.all(DesignTokens.spaceLg),
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(DesignTokens.spaceMd),
            decoration: BoxDecoration(
              color: DesignTokens.infoSoftBg,
              borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
              border: Border.all(color: DesignTokens.border),
            ),
            child: Text(
              tr.wifiCloudCommandsSeparated,
              style: theme.textTheme.bodySmall?.copyWith(
                color: DesignTokens.infoSoftText,
              ),
            ),
          ),
          const SizedBox(height: DesignTokens.spaceLg),
          switch (state.phase) {
            WifiProvisioningPhase.instructions => _Instructions(
                onContinue: vm.acknowledgePossession,
              ),
            WifiProvisioningPhase.form ||
            WifiProvisioningPhase.submitting =>
              _Form(
                state: state,
                submitting: state.phase == WifiProvisioningPhase.submitting,
                onSsid: vm.setSsid,
                onPassword: vm.setPassword,
                onSubmit: vm.submit,
              ),
            WifiProvisioningPhase.result => _Result(
                outcome: state.outcome!,
                onRetry: vm.retry,
                onBack: () =>
                    context.go(AppRoutes.deviceDetailPath(deviceId)),
              ),
          },
        ],
      ),
    );
  }
}

class _Instructions extends StatelessWidget {
  const _Instructions({required this.onContinue});

  final VoidCallback onContinue;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(tr.wifiPossessionSteps, style: Theme.of(context).textTheme.bodyMedium),
        const SizedBox(height: DesignTokens.spaceXl),
        ElevatedButton(
          onPressed: onContinue,
          child: Text(tr.wifiContinueToForm),
        ),
      ],
    );
  }
}

class _Form extends StatelessWidget {
  const _Form({
    required this.state,
    required this.submitting,
    required this.onSsid,
    required this.onPassword,
    required this.onSubmit,
  });

  final WifiProvisioningState state;
  final bool submitting;
  final ValueChanged<String> onSsid;
  final ValueChanged<String> onPassword;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(tr.wifiFormNotice, style: Theme.of(context).textTheme.bodySmall),
        const SizedBox(height: DesignTokens.spaceMd),
        TextField(
          enabled: !submitting,
          onChanged: onSsid,
          decoration: InputDecoration(labelText: tr.wifiSsidLabel),
        ),
        const SizedBox(height: DesignTokens.spaceMd),
        TextField(
          enabled: !submitting,
          obscureText: true,
          onChanged: onPassword,
          decoration: InputDecoration(labelText: tr.wifiPasswordLabel),
        ),
        if (state.errorMessage != null) ...[
          const SizedBox(height: DesignTokens.spaceMd),
          Text(
            state.errorMessage!,
            style: TextStyle(color: Theme.of(context).colorScheme.error),
          ),
        ],
        const SizedBox(height: DesignTokens.spaceXl),
        ElevatedButton(
          onPressed: submitting ? null : onSubmit,
          child: submitting
              ? const SizedBox(
                  height: 20,
                  width: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Text(tr.wifiSubmitLocal),
        ),
      ],
    );
  }
}

class _Result extends StatelessWidget {
  const _Result({
    required this.outcome,
    required this.onRetry,
    required this.onBack,
  });

  final LocalProvisioningOutcome outcome;
  final VoidCallback onRetry;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    final tr = context.tr;
    final (title, body) = switch (outcome) {
      LocalProvisioningOutcome.connected => (
          tr.wifiResultConnectedTitle,
          tr.wifiResultConnectedBody,
        ),
      LocalProvisioningOutcome.failed => (
          tr.wifiResultFailedTitle,
          tr.wifiResultFailedBody,
        ),
      LocalProvisioningOutcome.timedOut => (
          tr.wifiResultTimeoutTitle,
          tr.wifiResultTimeoutBody,
        ),
      LocalProvisioningOutcome.unavailable => (
          tr.wifiResultUnavailableTitle,
          tr.wifiResultUnavailableBody,
        ),
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(title, style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: DesignTokens.spaceMd),
        Text(body),
        const SizedBox(height: DesignTokens.spaceXl),
        if (outcome != LocalProvisioningOutcome.connected)
          ElevatedButton(onPressed: onRetry, child: Text(tr.retry)),
        TextButton(onPressed: onBack, child: Text(tr.back)),
      ],
    );
  }
}
